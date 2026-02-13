using DevHabit.Api.DTOs.Common;

namespace DevHabit.Api.Services;

/// <summary>
/// Responsible for generating HATEOAS links dynamically.
/// </summary>
public sealed class LinkService(
    LinkGenerator linkGenerator,
    IHttpContextAccessor httpContextAccessor)
{
    /// <summary>
    /// Creates a HATEOAS link for a specific endpoint.
    /// </summary>
    /// <param name="endpointName">Action/endpoint name</param>
    /// <param name="rel">Relationship (self, update, delete, etc.)</param>
    /// <param name="method">HTTP method (GET, POST, PUT, DELETE, etc.)</param>
    /// <param name="values">Route values</param>
    /// <param name="controller">Optional controller name</param>
    public LinkDto Create(
        string endpointName,
        string rel,
        string method,
        object? values = null,
        string? controller = null)
    {
        // Generate full absolute URI
        string? href = linkGenerator.GetUriByAction(
            httpContextAccessor.HttpContext!,
            endpointName,
            controller,
            values);

        return new LinkDto
        {
            // Throw exception if endpoint is invalid
            Href = href ?? throw new Exception("Invalid enpoint name provided"),
            Rel = rel,
            Method = method,
        };
    }
}
