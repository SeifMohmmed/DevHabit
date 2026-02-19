using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Github;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Services;

/// <summary>
/// Service responsible for securely storing, retrieving, and revoking
/// GitHub access tokens for users.
/// Tokens are encrypted before persistence.
/// </summary>
public class GitHubAccessTokenService(
    ApplicationDbContext context,
    EncryptionService encryptionService)
{
    /// <summary>
    /// Stores or updates a GitHub access token for a user.
    /// If a token already exists, it will be updated.
    /// Otherwise, a new record is created.
    /// </summary>
    public async Task StoreAsync(
        string userId,
        StoreGithubAccessTokenDto accessTokenDto,
        CancellationToken cancellationToken = default)
    {
        // Check if user already has a stored token
        GitHubAccessToken? existingAcessToken = await GetAccessTokenAsync(userId, cancellationToken);

        // Encrypt token before storing in database
        string encryptedToken = encryptionService.Encrypt(accessTokenDto.AccessToken);

        if (existingAcessToken is not null)
        {
            // Update existing token
            existingAcessToken.Token = encryptedToken;

            // Update expiration date
            existingAcessToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays);
        }
        else
        {
            // Create new token record
            context.GitHubAccessTokens.Add(new GitHubAccessToken
            {
                // Prefix helps identify token type
                Id = $"gh_{Guid.CreateVersion7()}",
                UserId = userId,
                Token = encryptedToken,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays)
            });
        }

        // Persist changes
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves and decrypts a user's GitHub access token.
    /// Returns null if no token exists.
    /// </summary>
    public async Task<string?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Fetch token record
        GitHubAccessToken? gitHubAccessToken = await GetAccessTokenAsync(userId, cancellationToken);

        if (gitHubAccessToken is null)
        {
            return null;
        }

        // Decrypt token before returning
        string decryptedToken = encryptionService.Decrypt(gitHubAccessToken.Token);

        return decryptedToken;
    }

    /// <summary>
    /// Revokes a user's GitHub token by removing it from the database.
    /// </summary>
    public async Task RevokeAsync(string userId, CancellationToken cancellationToken = default)
    {
        GitHubAccessToken? gitHubAccessToken = await GetAccessTokenAsync(userId, cancellationToken);

        if (gitHubAccessToken is null)
        {
            return;
        }

        // Remove token record
        context.GitHubAccessTokens.Remove(gitHubAccessToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves the GitHubAccessToken entity for a user.
    /// Internal helper method.
    /// </summary>
    private async Task<GitHubAccessToken?> GetAccessTokenAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await context.GitHubAccessTokens
            .FirstOrDefaultAsync(gh => gh.UserId == userId, cancellationToken);
    }
}
