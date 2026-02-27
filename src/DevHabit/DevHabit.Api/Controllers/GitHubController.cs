using DevHabit.Api.Common.Auth;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Github;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Controllers;
[Authorize(Roles = Roles.Member)]
[ApiController]
[Route("github")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class GitHubController(
    GitHubAccessTokenService gitHubAccessTokenService,
    RefitGitHubService gitHubService,
    UserContext userContext,
    LinkService linkService) : ControllerBase
{
    /// <summary>
    /// Stores a GitHub personal access token for the authenticated user.
    /// </summary>
    /// <param name="storeGithubAccessTokenDto">GitHub access token data.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Token stored successfully.</response>
    [HttpPut("personal-access-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StoreAccessToken(StoreGithubAccessTokenDto storeGithubAccessTokenDto)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await gitHubAccessTokenService.StoreAsync(userId, storeGithubAccessTokenDto);

        return NoContent();
    }

    /// <summary>
    /// Revokes the stored GitHub personal access token.
    /// </summary>
    /// <returns>No content.</returns>
    /// <response code="204">Token revoked successfully.</response>
    [HttpDelete("personal-access-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAccessToken()
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await gitHubAccessTokenService.RevokeAsync(userId);

        return NoContent();
    }

    /// <summary>
    /// Gets the authenticated user's GitHub profile.
    /// </summary>
    /// <param name="acceptHeaderDto">Accept header configuration.</param>
    /// <returns>The GitHub user profile.</returns>
    /// <response code="200">Profile retrieved successfully.</response>
    /// <response code="404">Access token or profile not found.</response>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(GitHubUserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile(AcceptHeaderDto acceptHeaderDto)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        string? accessToken = await gitHubAccessTokenService.GetAsync(userId);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return NotFound();
        }

        GitHubUserProfileDto? userProfile = await gitHubService.GetUserProfileAsync(accessToken);

        if (userProfile is null)
        {
            return NotFound();
        }

        if (acceptHeaderDto.IncludeLinks)
        {
            userProfile.Links =
            [
                linkService.Create(nameof(GetUserProfile), "self", HttpMethods.Get),
                linkService.Create(nameof(StoreAccessToken), "store-token", HttpMethods.Put),
                linkService.Create(nameof(RevokeAccessToken), "revoke-token", HttpMethods.Delete),
            ];
        }

        return Ok(userProfile);
    }

    /// <summary>
    /// Gets the authenticated user's GitHub events.
    /// </summary>
    /// <returns>List of GitHub events.</returns>
    /// <response code="200">Events retrieved successfully.</response>
    /// <response code="404">Profile or events not found.</response>
    [HttpGet("events")]
    [ProducesResponseType(typeof(IReadOnlyList<GitHubEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<GitHubEventDto>>> GetUserEvents()
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        string? accessToken = await gitHubAccessTokenService.GetAsync(userId);

        if (accessToken is null)
        {
            return Unauthorized();
        }

        GitHubUserProfileDto? userProfile = await gitHubService.GetUserProfileAsync(accessToken);

        if (userProfile is null)
        {
            return NotFound();
        }

        IReadOnlyList<GitHubEventDto?> events = await gitHubService.GetUserEventsAsync(
            userProfile.Login,
            accessToken);

        if (events is null)
        {
            return NotFound();
        }

        return Ok(events);
    }
}
