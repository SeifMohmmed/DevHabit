using System.Dynamic;
using System.Linq.Dynamic.Core;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace DevHabit.Api.Controllers;
[ApiController]
[Route("habits")]
public class HabitsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public HabitsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetHabits(
        [FromQuery] HabitsQueryParameters query,
        SortMappingProvider sortMappingProvider,
        DataShappingService dataShappingService)
    {
        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provider sort parameter isn't valid: '{query.Sort}'");
        }

        if (!dataShappingService.Validate<HabitDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provider data shapping fields aren't valid: '{query.Fields}'");
        }

        query.search ??= query.search?.Trim().ToLower();

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();


        IQueryable<HabitDto> habitsQuery = _dbContext
                    .Habits
                    .Where(h => query.search == null ||
                                    h.Name.ToLower().Contains(query.search) ||
                                    h.Description != null && h.Description.ToLower().Contains(query.search))
                    .Where(h => query.type == null || h.Type == query.type)
                    .Where(h => query.status == null || h.Status == query.status)
                     .ApplySort(query.Sort, sortMappings)
                    .Select(HabitQueries.ProjectToDto());

        int totalCount = await habitsQuery.CountAsync();

        List<HabitDto> habits = await habitsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShappingService.ShapeCollectionData(habits, query.Fields),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHabit(
        string id,
        string? fields,
        DataShappingService dataShappingService)
    {

        if (!dataShappingService.Validate<HabitWithTagsDto>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provider data shapping fields aren't valid: '{fields}'");
        }


        HabitWithTagsDto? habit = await _dbContext
                .Habits
                .Where(x => x.Id == id)
                .Select(HabitQueries.ProjectToDtoWithTags())
                .AsNoTracking()
                .FirstOrDefaultAsync();

        ExpandoObject shapedHabitDto = dataShappingService.ShapeData(habit, fields);

        return habit is null ? NotFound() : Ok(shapedHabitDto);
    }


    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator)
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

        Habit habit = createHabitDto.ToEntity();

        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(string id, [FromBody] UpdatedHabitDto updatedHabitDto)
    {
        Habit? habit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }
        habit.UpdateFromDto(updatedHabitDto);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
    {
        Habit? habit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        HabitDto habitDto = habit.ToDto();
        patchDocument.ApplyTo(habitDto, ModelState);

        if (!TryValidateModel(habitDto))
        {
            return ValidationProblem(ModelState);
        }

        habit.Name = habitDto.Name;
        habit.Description = habitDto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHabit(string id)
    {
        Habit? habit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        _dbContext.Remove(habit);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

}

