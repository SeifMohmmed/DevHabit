using CsvHelper.Configuration.Attributes;

namespace DevHabit.Api.Jobs;

public sealed record CsvEntryRecord
{
    [Name("habit_id")]
    public required string HabitId { get; init; }

    [Name("date")]
    public required DateTime Date { get; init; }

    [Name("notes")]
    public string? Notes { get; init; }
}
