using System.Security.Cryptography;
using System.Text;
using DevHabit.Api.Services;

namespace DevHabit.Api.Middleware;

/// <summary>
/// Middleware responsible for generating and validating ETags for HTTP responses.
/// Helps improve performance by enabling client-side caching.
/// </summary>
public sealed class ETagMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Main middleware pipeline execution method.
    /// Intercepts the response, generates ETag, and handles conditional requests.
    /// </summary>
    public async Task InvokeAsync(
            HttpContext context,
            InMemoryETagStore eTagStore)
    {
        // Skip ETag processing for non-cacheable methods
        if (CanSkipETag(context))
        {
            await next(context);
            return;
        }

        // Use request path as resource identifier
        string resourceUri = context.Request.Path.Value!;

        // Extract If-None-Match header (remove quotes)
        string? ifNoneMatch = context.Request.Headers.IfNoneMatch.FirstOrDefault()?.Replace("\"", "");

        // Backup original response stream
        Stream originalStream = context.Response.Body;

        // Replace response stream with memory stream to capture response body
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        // Continue pipeline
        await next(context);

        // Check if response is eligible for ETag
        if (IsETaggableResponse(context))
        {
            memoryStream.Position = 0;

            // Read response body as byte array
            byte[] responseBody = await GetResponseBody(memoryStream);

            // Generate hash-based ETag
            string etag = GenerateETag(responseBody);

            // Store generated ETag
            eTagStore.SetTag(resourceUri, etag);

            // Add ETag header to response
            context.Response.Headers.ETag = $"\"{etag}\"";

            // Restore original stream
            context.Response.Body = originalStream;

            // If client already has same version → return 304
            if (context.Request.Method == HttpMethods.Get && ifNoneMatch == etag)
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.ContentLength = 0;
                return;
            }
        }

        // Copy captured response back to original stream
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(originalStream);
    }

    /// <summary>
    /// Determines if response is eligible for ETag generation.
    /// Only applies to successful JSON responses.
    /// </summary>
    private static bool IsETaggableResponse(HttpContext context)
    {
        return context.Response.StatusCode == StatusCodes.Status200OK &&
            (context.Response.Headers.ContentType
            .FirstOrDefault()?
            .Contains("json", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    /// <summary>
    /// Reads response body from memory stream and returns it as byte array.
    /// </summary>
    private static async Task<byte[]> GetResponseBody(MemoryStream memoryStream)
    {
        using var reader = new StreamReader(memoryStream, leaveOpen: true);

        memoryStream.Position = 0;

        string content = await reader.ReadToEndAsync();

        return Encoding.UTF8.GetBytes(content);
    }

    /// <summary>
    /// Generates ETag by hashing response content using SHA512.
    /// </summary>
    private static string GenerateETag(byte[] content)
    {
        byte[] hash = SHA512.HashData(content);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Determines whether ETag processing should be skipped.
    /// Typically skip for POST and DELETE since they modify state.
    /// </summary>
    private static bool CanSkipETag(HttpContext context)
    {
        return context.Request.Method == HttpMethods.Post ||
            context.Request.Method == HttpMethods.Delete;
    }
}
