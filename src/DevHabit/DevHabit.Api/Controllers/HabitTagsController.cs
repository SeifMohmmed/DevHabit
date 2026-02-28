using DevHabit.Api.Common.Auth;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.HabitTags;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;
[Route("habits/{habitId}/tags")]
[ApiController]
[Authorize(Roles = Roles.Member)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class HabitTagsController(ApplicationDbContext context) : ControllerBase
{
    public static readonly string Name = nameof(HabitTagsController).Replace("Controller", string.Empty);

    /// <summary>
    /// Creates or updates tags for a habit.
    /// </summary>
    /// <param name="habitId">The habit unique identifier.</param>
    /// <param name="upsertHabitTagsDto">The tag identifiers to associate with the habit.</param>
    /// <returns>Status of the operation.</returns>
    /// <response code="200">Tags updated successfully.</response>
    /// <response code="204">No changes were required.</response>
    /// <response code="400">One or more tag IDs are invalid.</response>
    /// <response code="404">Habit not found.</response>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpsertHabitTags(string habitId, UpsertHabitTagsDto upsertHabitTagsDto)
    {
        Habit? habit = await context.Habits
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

        List<string> existingTagIds = await context
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

        await context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Removes a specific tag from a habit.
    /// </summary>
    /// <param name="habitId">The habit unique identifier.</param>
    /// <param name="tagId">The tag unique identifier.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Tag removed successfully.</response>
    /// <response code="404">Habit tag not found.</response>
    [HttpDelete("{tagId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteHabitTag(string habitId, string tagId)
    {
        HabitTag? habitTag = await context.HabitTags
            .SingleOrDefaultAsync(ht => ht.HabitId == habitId && ht.TagId == tagId);

        if (habitTag is null)
        {
            return NotFound();
        }

        context.HabitTags.Remove(habitTag);

        await context.SaveChangesAsync();

        return NoContent();
    }
}
