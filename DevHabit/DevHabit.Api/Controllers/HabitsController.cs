using System.Dynamic;
using System.Linq.Dynamic.Core;
using System.Net.Mime;
using Asp.Versioning;
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
[Route("habits")]
[ApiController]
[ApiVersion(1.0)]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.JsonV2,
    CustomMediaTypeNames.Application.HateoasJsonV1,
    CustomMediaTypeNames.Application.HateoasJsonV2)]
public class HabitsController(ApplicationDbContext dbContext, LinkService linkService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHabits(
        [FromQuery] HabitsQueryParameters query,
        SortMappingProvider sortMappingProvider,
        DataShapingService dataShappingService)
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


        IQueryable<HabitDto> habitsQuery = dbContext
                    .Habits
                    .Where(h => query.search == null ||
                                    h.Name.ToLower().Contains(query.search) ||
                                    h.Description != null && h.Description.ToLower().Contains(query.search))
                    .Where(h => query.Type == null || h.Type == query.Type)
                    .Where(h => query.Status == null || h.Status == query.Status)
                     .ApplySort(query.Sort, sortMappings)
                    .Select(HabitQueries.ProjectToDto());

        int totalCount = await habitsQuery.CountAsync();

        List<HabitDto> habits = await habitsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();


        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShappingService.ShapeCollectionData(
                habits,
                query.Fields,
                query.IncludeLinks ? h => CreateLinksForHabit(h.Id, query.Fields) : null),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };

        if (query.IncludeLinks)
        {
            paginationResult.Links = CreateLinksForHabits(
        query,
        paginationResult.HasNextPage,
        paginationResult.HasPreviousPage);
        }


        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    [MapToApiVersion(1.0)]
    public async Task<IActionResult> GetHabit(
        string id,
        [FromQuery] HabitsQueryParameters query,
        DataShapingService dataShappingService)
    {

        if (!dataShappingService.Validate<HabitWithTagsDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provider data shapping fields aren't valid: '{query.Fields}'");
        }


        HabitWithTagsDto? habit = await dbContext
                .Habits
                .Where(x => x.Id == id)
                .Select(HabitQueries.ProjectToDtoWithTags())
                .AsNoTracking()
                .FirstOrDefaultAsync();

        ExpandoObject shapedHabitDto = dataShappingService.ShapeData(habit, query.Fields);

        if (habit is null)
        {
            return NotFound();
        }

        if (query.IncludeLinks)
        {
            List<LinkDto> links = CreateLinksForHabit(id, query.Fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return Ok(shapedHabitDto);
    }

    [HttpGet("{id}")]
    [ApiVersion(2.0)]
    public async Task<IActionResult> GetHabitV2(
    string id,
    [FromHeader(Name ="Accept")]
    string? accept,
    string? fields,
    DataShapingService dataShappingService)
    {

        if (!dataShappingService.Validate<HabitWithTagsDtoV2>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provider data shapping fields aren't valid: '{fields}'");
        }


        HabitWithTagsDtoV2? habit = await dbContext
                .Habits
                .Where(x => x.Id == id)
                .Select(HabitQueries.ProjectToDtoWithTagsV2())
                .AsNoTracking()
                .FirstOrDefaultAsync();

        ExpandoObject shapedHabitDto = dataShappingService.ShapeData(habit, fields);

        if (accept == CustomMediaTypeNames.Application.HateoasJson)
        {
            List<LinkDto> links = CreateLinksForHabit(id, fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return habit is null ? NotFound() : Ok(shapedHabitDto);
    }


    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator)
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

        Habit habit = createHabitDto.ToEntity();

        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();

        habitDto.Links = CreateLinksForHabit(habit.Id, null);

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(string id, [FromBody] UpdatedHabitDto updatedHabitDto)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }
        habit.UpdateFromDto(updatedHabitDto);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

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

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHabit(string id)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        dbContext.Remove(habit);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    //For Collection Resource
    private List<LinkDto> CreateLinksForHabits(
        HabitsQueryParameters parameters,
        bool hashNextPage,
        bool hasPerviousPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetHabits), "self", HttpMethods.Get, new
        {
            page = parameters.Page,
            pageSize = parameters.PageSize,
            fields = parameters.Fields,
            q= parameters.search,
            sort = parameters.Sort,
              parameters.Type,
              parameters.Status
        }),
        linkService.Create(nameof(CreateHabit),"create",HttpMethods.Post)
        ];

        if (hashNextPage)
        {
            links.Add(linkService.Create(nameof(GetHabits), "next-page", HttpMethods.Get, new
            {
                page = parameters.Page + 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.search,
                sort = parameters.Sort,
                parameters.Type,
                parameters.Status
            }));
        }

        if (hasPerviousPage)
        {
            links.Add(linkService.Create(nameof(GetHabits), "pervious-page", HttpMethods.Get, new
            {
                page = parameters.Page - 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.search,
                sort = parameters.Sort,
                parameters.Type,
                parameters.Status
            }));
        }

        return links;
    }

    //For Single Resource
    private List<LinkDto> CreateLinksForHabit(string id, string? fields)
    {
        List<LinkDto> links =
           [
                linkService.Create(nameof(GetHabit),"self",HttpMethods.Get,new{id,fields}),
                linkService.Create(nameof(GetHabit),"update",HttpMethods.Put,new{id}),
                linkService.Create(nameof(GetHabit),"partial-update",HttpMethods.Patch,new{id}),
                linkService.Create(nameof(GetHabit),"delete",HttpMethods.Delete,new{id}),
                linkService.Create(
                    nameof(HabitTagsController.UpsertHabitTags),
                    "upsert-tags",
                    HttpMethods.Put,
                    new{habitId=id},
                    HabitTagsController.Name
                    ),

            ];

        return links;
    }

}

