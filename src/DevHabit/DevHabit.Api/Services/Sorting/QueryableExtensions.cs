using System.Linq.Dynamic.Core;
namespace DevHabit.Api.Services.Sorting;

/// <summary>
/// Provides IQueryable sorting extensions using dynamic LINQ.
/// </summary>
internal static class QueryableExtensions
{
    /// <summary>
    /// Applies dynamic sorting to query based on client input.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sort,
        SortMapping[] mappings,
        string defaultOrderBy = "Id")
    {
        // If no sorting specified → apply default ordering
        if (string.IsNullOrEmpty(sort))
        {
            return query.OrderBy(defaultOrderBy);
        }

        // Parse client sorting fields
        string[] sortFields = sort.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        var orderByParts = new List<string>();

        foreach (string field in sortFields)
        {
            // Extract field name and direction
            (string sortField, bool isDescending) = ParseSortField(field);

            // Find mapping definition
            SortMapping mapping = mappings.First(m =>
                m.SortField.Equals(sortField, StringComparison.OrdinalIgnoreCase));

            // Determine final sorting direction considering Reverse flag
            string direction = (isDescending, mapping.Reverse) switch
            {
                (false, false) => "ASC",
                (false, true) => "DESC",
                (true, false) => "DESC",
                (true, true) => "ASC",
            };

            // Build dynamic OrderBy expression
            orderByParts.Add($"{mapping.PropertyName} {direction}");
        }

        // Combine sorting expressions
        string orderBy = string.Join(",", orderByParts);

        return query.OrderBy(orderBy);
    }

    /// <summary>
    /// Parses sorting field and direction from client input.
    /// Example: "name desc"
    /// </summary>
    private static (string SortField, bool IsDescending) ParseSortField(string field)
    {
        string[] parts = field.Split(' ');

        string sortField = parts[0];

        bool isDescending =
            parts.Length > 1 &&
            parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortField, isDescending);
    }
}
