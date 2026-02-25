namespace DevHabit.Api.Services.Sorting;
/*
 Why Reverse?
 ------------------
 Some DTO fields represent computed values.

 Example:
 DTO exposes -> Age
 Entity stores -> DateOfBirth

 Sorting Age DESC requires:
 Sorting DateOfBirth ASC

 Reverse flag flips sorting direction automatically.
*/
/// <summary>
/// Defines mapping between client sort field and entity property.
/// </summary>
/// <param name="SortField">Field name exposed to client</param>
/// <param name="PropertyName">Actual entity property name</param>
/// <param name="Reverse">Indicates if sorting direction should be reversed</param>
public sealed record SortMapping(
    string SortField,
    string PropertyName,
    bool Reverse = false);
