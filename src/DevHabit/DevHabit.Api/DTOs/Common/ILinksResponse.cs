namespace DevHabit.Api.DTOs.Common;

/// <summary>
/// Represents response that contains HATEOAS links.
/// </summary>
public interface ILinksResponse
{
    List<LinkDto> Links { get; set; } // Navigation links related to resource.
}
