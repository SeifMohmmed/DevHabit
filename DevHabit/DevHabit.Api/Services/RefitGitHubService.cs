using DevHabit.Api.DTOs.Github;
using Refit;

namespace DevHabit.Api.Services;

/// <summary>
/// Service responsible for calling GitHub REST API.
/// Uses IHttpClientFactory with a named client.
/// </summary>
public sealed class RefitGitHubService(
    IGitHubApi gitHubApi,
    ILogger<GitHubService> logger)
{
    /// <summary>
    /// Retrieves authenticated user's GitHub profile.
    /// </summary>
    public async Task<GitHubUserProfileDto?> GetUserProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(accessToken);

        ApiResponse<GitHubUserProfileDto> response = await gitHubApi.GetUserProfile(accessToken, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user profile. Status code: {StatusCode}", response.StatusCode);
            return null;
        }

        return response.Content;
    }

    /// <summary>
    /// Retrieves public events for a specific GitHub user.
    /// </summary>
    public async Task<IReadOnlyList<GitHubEventDto>> GetUserEventsAsync(
           string username,
           string accessToken,
           int page,
           int perPage,
           CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(accessToken);
        ArgumentException.ThrowIfNullOrEmpty(username);

        ApiResponse<List<GitHubEventDto>> response = await gitHubApi.GetUserEvents(
            username,
            accessToken,
            page,
            perPage,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user events. Status code: {StatusCode}", response.StatusCode);
            return [];
        }

        return response.Content;
    }
}
