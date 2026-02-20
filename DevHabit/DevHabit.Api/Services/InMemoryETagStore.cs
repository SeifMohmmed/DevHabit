using System.Collections.Concurrent;

namespace DevHabit.Api.Services;

/// <summary>
/// Thread-safe in-memory store for managing ETag values per resource URI.
/// Used to cache generated ETags so they can be reused across requests.
/// </summary>
public sealed class InMemoryETagStore
{
    /// <summary>
    /// Static concurrent dictionary to store ETags keyed by resource URI.
    /// ConcurrentDictionary ensures thread safety for read/write operations.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> Etags = new();

    /// <summary>
    /// Gets the ETag for a given resource URI.
    /// If no ETag exists, initializes it with an empty string.
    /// </summary>
    /// <param name="resourceUri">The unique resource path.</param>
    /// <returns>The stored ETag value.</returns>
    public string GetTag(string resourceUri)
    {
        return Etags.GetOrAdd(resourceUri, _ => string.Empty);
    }

    /// <summary>
    /// Sets or updates the ETag for a given resource URI.
    /// </summary>
    /// <param name="resourceUri">The unique resource path.</param>
    /// <param name="etag">The generated ETag value.</param>
    public void SetTag(string resourceUri, string etag)
    {
        Etags.AddOrUpdate(resourceUri, etag, (_, _) => etag);
    }

    /// <summary>
    /// Removes the ETag associated with the specified resource URI.
    /// Useful when resource is deleted or invalidated.
    /// </summary>
    /// <param name="resourceUri">The unique resource path.</param>
    public void RemoveTag(string resourceUri)
    {
        Etags.TryRemove(resourceUri, out _);
    }
}
