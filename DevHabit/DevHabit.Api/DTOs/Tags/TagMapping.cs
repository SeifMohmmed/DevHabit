using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.Tags;

internal static class TagMapping
{
    public static TagDto ToDto(this Tag tag)
    {
        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            CreateAtUtc = tag.CreateAtUtc,
            UpdateAtUtc = tag.UpdateAtUtc,
        };
    }

    public static Tag ToEntity(this CreateTagDto dto)
    {
        Tag tag = new()
        {
            Id = $"t_{Guid.CreateVersion7()}",
            Name = dto.Name,
            Description = dto.Description,
            CreateAtUtc = DateTime.UtcNow
        };

        return tag;
    }

    public static void UpdateFromDto(this Tag tag, UpdateTagDto dto)
    {
        tag.Name = dto.Name;
        tag.Description = dto.Description;
        tag.UpdateAtUtc = DateTime.UtcNow;
    }
}
