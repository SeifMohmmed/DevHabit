namespace DevHabit.Api.Entities;

public sealed class Tag
{
    public string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreateAtUtc { get; set; }
    public DateTime? UpdateAtUtc { get; set; }
}
