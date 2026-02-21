using DevHabit.Api.DTOs.Common;

namespace DevHabit.Api.Common;

public sealed class CollectionResponse<T> : ICollectionResponse<T>, ILinksResponse
{
    public List<T> Items { get; init; }
    public List<LinkDto> Links { get; set; }
}
