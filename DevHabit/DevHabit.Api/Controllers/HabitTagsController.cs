using DevHabit.Api.Database;
using DevHabit.Api.DTOs.HabitTags;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;
[Route("habits/{habitId}/tags")]
[ApiController]
public sealed class HabitTagsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HabitTagsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPut]
    public async Task<ActionResult> UpsertHabitTags(string habitId, UpsertHabitTagsDto upsertHabitTagsDto)
    {
        Habit? habit = await _context.Habits
            .Include(h => h.HabitTags)
            .FirstOrDefaultAsync(h => h.Id == habitId);

        if (habit is null)
        {
            return NotFound();
        }

        var currentTagIds = habit.HabitTags.Select(ht => ht.TagId).ToHashSet();

        if (currentTagIds.SetEquals(upsertHabitTagsDto.TagsIds))
        {
            return NoContent();
        }

        List<string> existingTagIds = await _context
            .Tags
            .Where(t => upsertHabitTagsDto.TagsIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        if (existingTagIds.Count != upsertHabitTagsDto.TagsIds.Count)
        {
            return BadRequest("One or more tag IDs is invalid");
        }

        habit.HabitTags.RemoveAll(ht => !upsertHabitTagsDto.TagsIds.Contains(ht.TagId));

        string[] tagIdsToAdd = upsertHabitTagsDto.TagsIds.Except(currentTagIds).ToArray();

        habit.HabitTags.AddRange(tagIdsToAdd.Select(tagId => new HabitTag
        {
            HabitId = habitId,
            TagId = tagId,
            CreatedAtUtc = DateTime.UtcNow,
        }));

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{tagId}")]
    public async Task<ActionResult> DeleteHabitTag(string habitId, string tagId)
    {
        HabitTag? habitTag = await _context.HabitTags
            .SingleOrDefaultAsync(ht => ht.HabitId == habitId && ht.TagId == tagId);

        if (habitTag is null)
        {
            return NotFound();
        }

        _context.HabitTags.Remove(habitTag);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
