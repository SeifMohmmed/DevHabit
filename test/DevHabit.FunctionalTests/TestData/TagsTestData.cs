using DevHabit.Api.DTOs.Tags;

namespace DevHabit.FunctionalTests.TestData;
public static class TagsTestData
{
    public static CreateTagDto CreateTagDto() => new()
    {
        Name = "Productivity",
        Description = "Productivity related habits"
    };
}