namespace DevHabit.Api.Services.Sorting;
/*
 Why Reverse?
Some fields represent computed values.

AGE -> Entity stores: DateOfBirth

If sorting by Age descending:

We must sort DateOfBirth ascending.

Reverse flag allows this behavior.
*/
public sealed record SortMapping(string SortField, string PropertyName, bool Reverse = false);
