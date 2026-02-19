namespace DevHabit.Api.Entities;

/// <summary>
/// Represents stored GitHub access token for a user.
/// Token should be encrypted before persistence.
/// </summary>
public sealed class GitHubAccessToken
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
