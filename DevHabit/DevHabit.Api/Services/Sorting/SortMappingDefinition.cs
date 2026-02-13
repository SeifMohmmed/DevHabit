namespace DevHabit.Api.Services.Sorting;

/// <summary>
/// Represents sorting mapping definition between DTO and Entity.
/// </summary>
/// <typeparam name="TSource">DTO type</typeparam>
/// <typeparam name="TDestination">Entity type</typeparam>
public sealed class SortMappingDefinition<TSource, TDestination> : ISortMappingDefinition
{
    public required SortMapping[] Mappings { get; init; }
}
