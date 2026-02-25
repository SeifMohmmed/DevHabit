using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DevHabit.IntegrationTests.Tests;
public sealed class HabtisTests(DevHabitWebAppFactory factory) : IntegrationTestFixture(factory)
{
    public static readonly CreateHabitDto ValidCreateHabitDto = new()
    {
        Name = "Read books",
        Description = "Read technical books to improve skills",
        Type = HabitType.Measurable,
        Frequency = new()
        {
            Type = FrequancyType.Daily,
            TimesPerPeriod = 1,
        },
        Target = new()
        {
            Value = 30,
            Unit = "pages",
        },
    };

    public static readonly CreateHabitDto InValidCreateHabitDto = new()
    {
        Name = "Read books",
        Description = "Read technical books to improve skills",
        Type = HabitType.Binary,
        Frequency = new()
        {
            Type = FrequancyType.None,
            TimesPerPeriod = -1,
        },
        Target = new()
        {
            Value = 30,
            Unit = "pages",
        },
    };

    public static readonly UpdatedHabitDto ValidUpdateHabitDto = new()
    {
        Name = "Updated Habit",
        Description = "Updated Description",
        Type = HabitType.Measurable,
        Frequency = new()
        {
            Type = FrequancyType.Weekly,
            TimesPerPeriod = 3,
        },
        Target = new()
        {
            Value = 50,
            Unit = "pages",
        },
    };

    public static readonly UpdatedHabitDto InValidUpdateHabitDto = new()
    {
        Name = "Updated Habit",
        Description = "Updated Description",
        Type = HabitType.Binary,
        Frequency = new()
        {
            Type = FrequancyType.None,
            TimesPerPeriod = -1,
        },
        Target = new()
        {
            Value = 50,
            Unit = "pages",
        },
    };

    [Fact]
    public async Task GetHabits_ShouldReturnEmptyList_WhenNoHabitsExist()
    {
        //Arrange
        await CleanUpDatabaseAsync();

        HttpClient client = await CreateAuthenticatedClientAsync();

        //Act

        HttpResponseMessage response = await client.GetAsync(Routes.Habits.GetAll);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetHabits_ShouldReturnHabits_WhenHabitsExist()
    {
        //Arrange
        await CleanUpDatabaseAsync();

        HttpClient client = await CreateAuthenticatedClientAsync();

        //Create a habit first
        CreateHabitDto createDto = ValidCreateHabitDto;
        await client.PostAsJsonAsync(Routes.Habits.Create, createDto);


        //Act
        HttpResponseMessage response = await client.GetAsync(Routes.Habits.GetAll);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(createDto.Name, result.Items[0].Name);
    }

    [Fact]
    public async Task GetHabits_ShouldSupportFiltering()
    {
        //Arrange
        await CleanUpDatabaseAsync();

        HttpClient client = await CreateAuthenticatedClientAsync();

        //Create a habit first
        CreateHabitDto measurableHabit = ValidCreateHabitDto;
        CreateHabitDto binaryHabit = InValidCreateHabitDto;
        binaryHabit = binaryHabit with { Type = HabitType.Binary };

        await client.PostAsJsonAsync(Routes.Habits.Create, measurableHabit);
        await client.PostAsJsonAsync(Routes.Habits.Create, binaryHabit);

        //Act
        HttpResponseMessage response = await client.GetAsync(
           $"{Routes.Habits.GetAll}?type={(int)HabitType.Measurable}");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(HabitType.Measurable, result.Items[0].Type);
    }

    [Fact]
    public async Task GetHabits_ShouldSupportSorting()
    {
        //Arrange
        await CleanUpDatabaseAsync();

        HttpClient client = await CreateAuthenticatedClientAsync();

        CreateHabitDto[] habits =
[
    new CreateHabitDto
    {
        Name = "Z Habit",
        Type = HabitType.Measurable,
        Frequency = new FrequencyDto
        {
            Type = FrequancyType.Daily,
            TimesPerPeriod = 1
        },
        Target = new TargetDto
        {
            Value = 30,
            Unit = "pages"
        }
    },
    new CreateHabitDto
    {
        Name = "A Habit",
        Type = HabitType.Measurable,
        Frequency = new FrequencyDto
        {
            Type = FrequancyType.Daily,
            TimesPerPeriod = 1
        },
        Target = new TargetDto
        {
            Value = 30,
            Unit = "pages"
        }
    }
];

        foreach (CreateHabitDto habit in habits)
        {
            await client.PostAsJsonAsync(Routes.Habits.Create, habit);
        }

        HttpResponseMessage response = await client.GetAsync(
   $"{Routes.Habits.GetAll}?sort=name");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("A Habit", result.Items[0].Name);
        Assert.Equal("Z Habit", result.Items[1].Name);
    }
}
