using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.DTOs.Entires;

[ValidateNever]
public sealed record CreateEntryDto
{
    public required string HabitId { get; init; }
    public required int Value { get; init; }
    public string? Notes { get; init; }
    public required DateTime Date { get; init; }
}
