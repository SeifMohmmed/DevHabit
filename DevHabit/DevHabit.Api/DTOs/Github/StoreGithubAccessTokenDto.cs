using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.DTOs.Github;

/// <summary>
/// DTO used to store encrypted GitHub access token.
/// </summary>
[ValidateNever]
public sealed record StoreGithubAccessTokenDto
{
    public required string AccessToken { get; init; }

    // Number of days before token expiration
    public required int ExpiresInDays { get; init; }
}
