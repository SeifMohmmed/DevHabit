namespace DevHabit.Api.DTOs.Common;

/// <summary>
/// Represents a HATEOAS navigation link.
/// </summary>
public sealed class LinkDto
{
    public required string Href { get; init; } // Fully qualified URI of the resource.
    public required string Rel { get; init; } // Relationship of the link (self, next, previous, update, delete, etc.).
    public required string Method { get; init; } // HTTP method used to access the link.
}
