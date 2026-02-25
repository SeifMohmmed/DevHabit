using DevHabit.Api.DTOs.Common;

namespace DevHabit.Api.DTOs.Entires;

public sealed record EntryParameters : AcceptHeaderDto
{
    public string? Fields { get; init; }
}
