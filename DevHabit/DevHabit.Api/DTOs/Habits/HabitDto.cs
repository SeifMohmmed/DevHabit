using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.Habits;

public sealed record HabitsCollectionDto(List<HabitDto> Data);
public sealed record HabitDto(
    string Id,
    string Name,
    string? Description,
    HabitType Type,
    FrequancyDto Frequancy,
    TargetDto Target,
    HabitStatus Status,
    bool IsArchived,
    DateOnly? EndDate,
    MilestoneDto? Milestone,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? LastCompletedAtUtc);
public sealed record FrequancyDto(
    FrequancyType Type,
    int TimesPerPeriond);

public sealed record TargetDto(
   int Value,
   string Unit);
public sealed record MilestoneDto(
    int Target,
    int Current);
