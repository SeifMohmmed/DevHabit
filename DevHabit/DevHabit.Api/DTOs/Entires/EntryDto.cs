using DevHabit.Api.DTOs.Common;
using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.Entires;

public class EntryDto
{
    public required string Id { get; init; }
    public required int Value { get; init; }
    public string? Notes { get; init; }
    public required EntrySource Source { get; init; }
    public string? ExternalId { get; init; }
    public required bool IsArchived { get; init; }
    public required DateTime Date { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public List<LinkDto> Links { get; set; }
}
