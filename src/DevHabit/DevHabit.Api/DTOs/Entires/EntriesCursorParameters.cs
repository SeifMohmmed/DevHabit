using DevHabit.Api.DTOs.Common;
using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.Entires;

public sealed record EntriesCursorParameters : AcceptHeaderDto
{
    public string? Cursor { get; init; }
    public string? Direction { get; init; } = "next"; // next | previous
    public string? Fields { get; init; }
    public string? HabitId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public EntrySource? Source { get; init; }
    public bool? IsArchived { get; init; }
    public int Limit { get; init; } = 10;
}
