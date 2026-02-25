namespace DevHabit.Api.DTOs.Entires;

public sealed record UpdateEntryDto
{
    public required int Value { get; init; }
    public string? Notes { get; init; }
}
