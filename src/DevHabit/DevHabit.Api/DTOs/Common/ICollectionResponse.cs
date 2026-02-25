namespace DevHabit.Api.DTOs.Common;

/// <summary>
/// Represents collection response contract.
/// Allows generic handling of collection results.
/// </summary>
public interface ICollectionResponse<T>
{
    List<T> Items { get; init; }
}
