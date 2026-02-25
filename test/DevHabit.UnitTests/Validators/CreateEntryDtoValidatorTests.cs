using DevHabit.Api.DTOs.Entires;
using DevHabit.Api.Entities;
using FluentValidation.Results;
using System;
using System.Threading.Tasks;

namespace DevHabit.UnitTests.Validators;

public sealed class CreateEntryDtoValidatorTests
{
    private readonly CreateEntryDtoValidator _validator = new();

    //Method_ShouldBehavior_WhenCondition
    [Fact]
    public async Task Validate_ShouldSucceed_WhenInputDtoIsValid()
    {
        //Arrange

        var dto = new CreateEntryDto
        {
            HabitId = Habit.CreateNewId(),
            Value = 1,
            Date = DateTime.UtcNow
        };

        //Act
        ValidationResult validationResult = await _validator.ValidateAsync(dto);

        //Assert 
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
    }
    [Fact]
    public async Task Validate_ShouldFail_WhenHabitIdIsEmpty()
    {
        //Arrange

        var dto = new CreateEntryDto
        {
            HabitId = string.Empty,
            Value = 1,
            Date = DateTime.UtcNow
        };

        //Act
        ValidationResult validationResult = await _validator.ValidateAsync(dto);

        //Assert 
        Assert.False(validationResult.IsValid);
        ValidationFailure validationFailure = Assert.Single(validationResult.Errors);

        Assert.Equal(nameof(CreateEntryDto.HabitId), validationFailure.PropertyName);
    }
}
