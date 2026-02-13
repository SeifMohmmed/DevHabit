using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.DTOs.Common;

/// <summary>
/// Represents paginated response returned from collection endpoints.
/// Supports HATEOAS links and pagination metadata.
/// </summary>
/// <typeparam name="T">Type of items returned in the collection</typeparam>
public sealed record PaginationResult<T> : ICollectionResponse<T>, ILinksResponse
{
    public List<T> Items { get; init; } // Collection items for current page.

    public int Page { get; init; } // Current page number.

    public int PageSize { get; init; } // Number of items per page.

    public long TotalCount { get; init; } // Total number of items in entire dataset.

    public List<LinkDto> Links { get; set; }  // HATEOAS links related to pagination and navigation.


    // Total number of pages calculated from TotalCount and PageSize.
    public long TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1; // Indicates whether previous page exists.

    public bool HasNextPage => Page < TotalPages; // Indicates whether next page exists.


    /// <summary>
    /// Creates pagination result asynchronously from IQueryable source.
    /// </summary>
    /// <param name="query">Queryable source</param>
    /// <param name="page">Requested page number</param>
    /// <param name="pageSize">Requested page size</param>
    public static async Task<PaginationResult<T>> CreateAsync(
        IQueryable<T> query,
        int page,
        int pageSize)
    {
        // Retrieve total item count
        int totalCount = await query.CountAsync();

        // Retrieve items for requested page
        List<T> items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginationResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
