using DevHabit.Api.DTOs.Entires;
using System;

namespace DevHabit.FunctionalTests.TestData;
public static class EntriesTestData
{
    public static CreateEntryDto CreateEntryDto(
        string habitId,
        int value,
        DateOnly? date = null,
        string? notes = null)
    {
        return new()
        {
            HabitId = habitId,
            Date = DateTime.UtcNow,
            Notes = notes,
            Value = value,
        };
    }
}