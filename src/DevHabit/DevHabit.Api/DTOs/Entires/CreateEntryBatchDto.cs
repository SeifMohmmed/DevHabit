namespace DevHabit.Api.DTOs.Entires;

public sealed record CreateEntryBatchDto
{
    public required List<CreateEntryDto> Entries { get; init; }
}
