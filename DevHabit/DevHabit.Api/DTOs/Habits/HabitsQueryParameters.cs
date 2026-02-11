using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.DTOs.Habits;

public sealed record HabitsQueryParameters
{
    [FromQuery(Name = "q")]
    public string? search { get; set; }
    public HabitType? type { get; init; }
    public HabitStatus? status { get; init; }
    public string? Sort { get; set; }
}
