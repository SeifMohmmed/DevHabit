namespace DevHabit.Api.DTOs.Github;

/// <summary>
/// Represents a GitHub event returned from the API.
/// </summary>
public sealed record GitHubEventDto(
    string Id,
    string Type,
    GitHubEventActorDto Actor,
    GitHubEventRepoDto Repo,
    GitHubEventPayloadDto Payload,
    bool Public,
    DateTimeOffset CreatedAt);

/// <summary>
/// Actor who triggered the event.
/// </summary>
public sealed record GitHubEventActorDto(
    long Id,
    string Login,
    string DisplayLogin,
    string GravatarId,
    Uri Url,
    Uri AvatarUrl);

/// <summary>
/// Repository related to event.
/// </summary>
public sealed record GitHubEventRepoDto(
    long Id,
    string Name,
    Uri Url);

/// <summary>
/// Additional event details.
/// </summary>
public sealed record GitHubEventPayloadDto(string Action);
