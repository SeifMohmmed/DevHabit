using DevHabit.Api.DTOs.Entires;
using DevHabit.Api.Entities;
using FluentValidation.TestHelper;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevHabit.UnitTests.Validators;
public sealed class CreateEntryBatchDtoValidatorTests
{
    private readonly CreateEntryBatchDtoValidator _validator;
    private readonly CreateEntryDtoValidator _entryValidator = new();

    public CreateEntryBatchDtoValidatorTests()
    {
        _validator = new(_entryValidator);
    }

    [Fact]
    public async Task Validate_ShouldNotReturnError_WhenAllPropertiesAreValid()
    {
        // Arrange
        CreateEntryBatchDto dto = new()
        {
            Entries =
            [
                new()
                {
                    HabitId = Habit.CreateNewId(),
                    Value = 1,
                    Date = DateTime.UtcNow
                },
            ],
        };

        // Act
        TestValidationResult<CreateEntryBatchDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenEntriesIsEmpty()
    {
        // Arrange
        CreateEntryBatchDto dto = new()
        {
            Entries = [],
        };

        // Act
        TestValidationResult<CreateEntryBatchDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Entries);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenEntriesExceedMaxBatchSize()
    {
        // Arrange
        CreateEntryBatchDto dto = new()
        {
            Entries = Enumerable.Range(0, 21).Select(_ => new CreateEntryDto()
            {
                HabitId = Habit.CreateNewId(),
                Value = 1,
                Date = DateTime.UtcNow,
            }).ToList(),
        };

        // Act
        TestValidationResult<CreateEntryBatchDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Entries);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenAnyEntryIsInvalid()
    {
        // Arrange
        CreateEntryBatchDto dto = new()
        {
            Entries =
            [
                new()
                {
                    HabitId = string.Empty,
                    Value = 1,
                    Date = DateTime.UtcNow,
                },
            ],
        };

        // Act
        TestValidationResult<CreateEntryBatchDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Entries[0].HabitId");
    }
}
