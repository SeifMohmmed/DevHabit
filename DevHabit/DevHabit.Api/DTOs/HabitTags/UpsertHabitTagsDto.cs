namespace DevHabit.Api.DTOs.HabitTags;

public sealed record UpsertHabitTagsDto
{
    public required List<string> TagsIds { get; init; }
}
