using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DevHabit.Api.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to simplify access to common claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Retrieves the IdentityId (NameIdentifier claim) from the current user principal.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
    /// <returns>
    /// The IdentityId string if present; otherwise null.
    /// </returns>
    public static string? GetIdentityId(this ClaimsPrincipal? principal)
    {
        // Find the NameIdentifier claim (usually the unique user ID from Identity provider)
        string? identityId = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return identityId;
    }
}
