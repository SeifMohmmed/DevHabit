using DevHabit.Api.DTOs.Common;

namespace DevHabit.Api.DTOs.Github;
/// <summary>
/// Represents detailed GitHub user profile information.
/// </summary>
public sealed record class GitHubUserProfileDto
{
    public string Login { get; init; }
    public long Id { get; init; }
    public string NodeId { get; init; }
    public Uri AvatarUrl { get; init; }
    public string GravatarId { get; init; }
    public Uri Url { get; init; }
    public Uri HtmlUrl { get; init; }
    public Uri FollowersUrl { get; init; }
    public Uri FollowingUrl { get; init; }
    public Uri GistsUrl { get; init; }
    public Uri StarredUrl { get; init; }
    public Uri SubscriptionsUrl { get; init; }
    public Uri OrganizationsUrl { get; init; }
    public Uri ReposUrl { get; init; }
    public Uri EventsUrl { get; init; }
    public Uri ReceivedEventsUrl { get; init; }
    public string Type { get; init; }
    public bool SiteAdmin { get; init; }
    public string Name { get; init; }
    public string Company { get; init; }
    public Uri Blog { get; init; }
    public string Location { get; init; }
    public string Email { get; init; }
    public bool? Hireable { get; init; }
    public string Bio { get; init; }
    public string TwitterUsername { get; init; }
    public long PublicRepos { get; init; }
    public long PublicGists { get; init; }
    public long Followers { get; init; }
    public long Following { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public long PrivateGists { get; init; }
    public long TotalPrivateRepos { get; init; }
    public long OwnedPrivateRepos { get; init; }
    public long DiskUsage { get; init; }
    public long Collaborators { get; init; }
    public bool TwoFactorAuthentication { get; init; }
    public GitHubUserProfilePlanDto Plan { get; init; }
    public List<LinkDto> Links { get; set; } // Custom links mapped internally.
}

public sealed record GitHubUserProfilePlanDto(
    string Name,
    long Space,
    long PrivateRepos,
    long Collaborators);
