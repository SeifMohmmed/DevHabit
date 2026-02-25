using DevHabit.Api.Database;
using DevHabit.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DevHabit.Api.Services;

/// <summary>
/// Provides access to the current application's user context,
/// including retrieving the internal UserId mapped from IdentityId.
/// </summary>
public sealed class UserContext(
    IHttpContextAccessor httpContextAccessor,
    ApplicationDbContext dbContext,
    IMemoryCache memoryCache)
{
    // Prefix used for cache keys to avoid collisions
    private const string CacheKeyPrefix = "users:id:";

    // Sliding cache duration (extends if accessed again within this time)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Retrieves the application UserId for the currently authenticated user.
    /// Uses in-memory caching to reduce database lookups.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The UserId if found; otherwise null (e.g., user not authenticated).
    /// </returns>
    public async Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        // Get IdentityId from the current HTTP context user claims
        string identityId = httpContextAccessor.HttpContext?.User.GetIdentityId();

        // If no identity is found, user is not authenticated
        if (identityId is null)
        {
            return null;
        }

        // Build cache key using prefix + identityId
        string cacheKey = $"{CacheKeyPrefix}{identityId}";

        // Try to get from cache, otherwise fetch from database
        string? userId = await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            // Set sliding expiration so cache renews on access
            entry.SetSlidingExpiration(CacheDuration);

            // Query database to map IdentityId -> UserId
            string userId = await dbContext.Users
                .Where(u => u.IdentityId == identityId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return userId;
        });

        return userId;
    }
}
