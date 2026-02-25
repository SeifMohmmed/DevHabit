using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.Habits;

public sealed record UpdatedHabitDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required HabitType Type { get; init; }
    public required FrequencyDto Frequency { get; init; }
    public required TargetDto Target { get; init; }
    public DateOnly? EndDate { get; init; }
    public UpdatedMilestoneDto? Milestone { get; init; }
}
public sealed class UpdatedMilestoneDto
{
    public required int Target { get; init; }
}
