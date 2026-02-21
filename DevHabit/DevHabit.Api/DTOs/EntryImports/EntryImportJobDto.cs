using DevHabit.Api.DTOs.Common;
using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.EntryImports;

public sealed record EntryImportJobDto
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required EntryImportStatus Status { get; init; }
    public required string FileName { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public List<LinkDto> Links { get; set; }
}
