using DevHabit.Api.DTOs.Github;
using Newtonsoft.Json;

namespace DevHabit.Api.Services;

/// <summary>
/// Service responsible for calling GitHub REST API.
/// Uses IHttpClientFactory with a named client.
/// </summary>
public sealed class GitHubService(
    IHttpClientFactory httpClientFactory,
    ILogger<GitHubService> logger)
{
    /// <summary>
    /// Retrieves authenticated user's GitHub profile.
    /// </summary>
    public async Task<GitHubUserProfileDto?> GetUserProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using HttpClient client = CreateGitHubClient(accessToken);

        HttpResponseMessage response = await client.GetAsync("user", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user profile. Status code: {StatusCode}", response.StatusCode);
            return null;
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonConvert.DeserializeObject<GitHubUserProfileDto>(content);
    }

    /// <summary>
    /// Retrieves public events for a specific GitHub user.
    /// </summary>
    public async Task<IReadOnlyList<GitHubEventDto>> GetUserEventsAsync(
        string username,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);

        using HttpClient client = CreateGitHubClient(accessToken);

        HttpResponseMessage response = await client.GetAsync(
            $"users/{username}/events?per_page=100",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user events. Status code: {StatusCode}", response.StatusCode);
            return [];
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonConvert.DeserializeObject<List<GitHubEventDto>>(content) ?? [];
    }

    /// <summary>
    /// Creates configured HttpClient with Bearer token.
    /// </summary>
    private HttpClient CreateGitHubClient(string accessToken)
    {
        HttpClient client = httpClientFactory.CreateClient("github");

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        return client;
    }
}
