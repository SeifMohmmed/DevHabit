using DevHabit.Api.DTOs.HabitTags;
using System.Collections.Generic;

namespace DevHabit.FunctionalTests.TestData;
public static class HabitTagsTestData
{
    public static UpsertHabitTagsDto CreateUpsertDto(List<string> tagIds) => new()
    {
        TagsIds = tagIds,
    };
}
