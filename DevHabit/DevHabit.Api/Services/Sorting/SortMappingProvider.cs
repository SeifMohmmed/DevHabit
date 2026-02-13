using System.Linq.Dynamic.Core;
namespace DevHabit.Api.Services.Sorting;

/// <summary>
/// Provides sorting mappings between DTO fields and Entity properties.
/// Used to translate client sort parameters into database-safe order expressions.
/// </summary>
public sealed class SortMappingProvider(
    IEnumerable<ISortMappingDefinition> sortMappingDefinitions)
{
    /// <summary>
    /// Retrieves sorting mappings between source (DTO) and destination (Entity).
    /// </summary>
    /// <typeparam name="TSource">DTO type requested by client</typeparam>
    /// <typeparam name="TDestination">Entity type stored in database</typeparam>
    /// <returns>Array of defined SortMapping</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when mapping definition is not registered
    /// </exception>
    public SortMapping[] GetMappings<TSource, TDestination>()
    {
        SortMappingDefinition<TSource, TDestination>? sortMappingDefinition =
            sortMappingDefinitions
                .OfType<SortMappingDefinition<TSource, TDestination>>()
                .FirstOrDefault();

        if (sortMappingDefinition is null)
        {
            throw new InvalidOperationException(
                $"The mapping from '{typeof(TSource).Name}' into '{typeof(TDestination).Name}' isn't defined");
        }

        return sortMappingDefinition.Mappings.ToArray();
    }

    /// <summary>
    /// Validates that requested sorting fields exist in mapping definitions.
    /// Prevents invalid or malicious sorting requests.
    /// </summary>
    public bool ValidateMappings<TSource, TDestination>(string? sort)
    {
        // If no sorting requested → valid
        if (string.IsNullOrWhiteSpace(sort))
        {
            return true;
        }

        // Extract field names ignoring direction (ASC / DESC)
        var sortFields = sort
            .Split(',')
            .Select(f => f.Trim().Split(' ')[0])
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        SortMapping[] mappings = GetMappings<TSource, TDestination>();

        // Ensure every requested field exists in mapping configuration
        return sortFields.All(field =>
            mappings.Any(m =>
                m.SortField.Equals(field, StringComparison.OrdinalIgnoreCase)));
    }
}
