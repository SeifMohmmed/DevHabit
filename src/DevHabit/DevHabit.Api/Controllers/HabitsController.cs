using System.Dynamic;
using System.Linq.Dynamic.Core;
using System.Net.Mime;
using Asp.Versioning;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
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
[Authorize(Roles = Roles.Member)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class HabitsController(
    ApplicationDbContext dbContext,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{
    /// <summary>
    /// Gets paginated habits for the authenticated user.
    /// </summary>
    /// <param name="query">Filtering and pagination parameters.</param>
    /// <param name="sortMappingProvider">Sorting provider service.</param>
    /// <param name="dataShappingService">Data shaping service.</param>
    /// <returns>Paginated list of habits.</returns>
    /// <response code="200">Habits retrieved successfully.</response>
    /// <response code="400">Invalid sorting or data shaping parameters.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResult<ExpandoObject>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHabits(
        [FromQuery] HabitsQueryParameters query,
        SortMappingProvider sortMappingProvider,
        DataShapingService dataShappingService)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

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
                    .Where(h => h.UserId == userId)
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

    /// <summary>
    /// Gets habit by id.
    /// </summary>
    /// <param name="id">The habit unique identifier.</param>
    /// <param name="query">Query parameters.</param>
    /// <param name="dataShappingService">Data shaping service.</param>
    /// <returns>The habit details.</returns>
    /// <response code="200">Habit retrieved successfully.</response>
    /// <response code="400">Invalid data shaping fields.</response>
    /// <response code="404">Habit not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExpandoObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MapToApiVersion(1.0)]
    public async Task<IActionResult> GetHabit(
        string id,
        [FromQuery] HabitsQueryParameters query,
        DataShapingService dataShappingService)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShappingService.Validate<HabitWithTagsDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provider data shapping fields aren't valid: '{query.Fields}'");
        }


        HabitWithTagsDto? habit = await dbContext
                .Habits
                .Where(x => x.Id == id && x.UserId == userId)
                .Select(HabitQueries.ProjectToDtoWithTags())
                .AsNoTracking()
                .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        ExpandoObject shapedHabitDto = dataShappingService.ShapeData(habit, query.Fields);


        if (query.IncludeLinks)
        {
            List<LinkDto> links = CreateLinksForHabit(id, query.Fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return Ok(shapedHabitDto);
    }

    /// <summary>
    /// Gets habit by id (API v2).
    /// </summary>
    /// <param name="id">The habit unique identifier.</param>
    /// <param name="accept">Accept header value.</param>
    /// <param name="fields">Data shaping fields.</param>
    /// <param name="dataShappingService">Data shaping service.</param>
    /// <returns>The habit details.</returns>
    /// <response code="200">Habit retrieved successfully.</response>
    /// <response code="400">Invalid data shaping fields.</response>
    /// <response code="404">Habit not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(HabitWithTagsDtoV2), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ApiVersion(2.0)]
    public async Task<IActionResult> GetHabitV2(
    string id,
    [FromHeader(Name ="Accept")]
    string? accept,
    string? fields,
    DataShapingService dataShappingService)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShappingService.Validate<HabitWithTagsDtoV2>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provider data shapping fields aren't valid: '{fields}'");
        }


        HabitWithTagsDtoV2? habit = await dbContext
                .Habits
                .Where(x => x.Id == id && x.UserId == userId)
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


    /// <summary>
    /// Creates a new habit.
    /// </summary>
    /// <param name="createHabitDto">Habit creation data.</param>
    /// <param name="validator">Habit validator.</param>
    /// <returns>The created habit.</returns>
    /// <response code="201">Habit created successfully.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType(typeof(HabitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createHabitDto);

        Habit habit = createHabitDto.ToEntity(userId);

        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();

        habitDto.Links = CreateLinksForHabit(habit.Id, null);

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    /// <summary>
    /// Updates an existing habit.
    /// </summary>
    /// <param name="id">The habit unique identifier.</param>
    /// <param name="updatedHabitDto">Updated habit data.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Habit updated successfully.</response>
    /// <response code="404">Habit not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateHabit(string id, [FromBody] UpdatedHabitDto updatedHabitDto)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

        if (habit is null)
        {
            return NotFound();
        }
        habit.UpdateFromDto(updatedHabitDto);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Partially updates a habit.
    /// </summary>
    /// <param name="id">The habit unique identifier.</param>
    /// <param name="patchDocument">JSON patch document.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Habit updated successfully.</response>
    /// <response code="400">Invalid patch document.</response>
    /// <response code="404">Habit not found.</response>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

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

    /// <summary>
    /// Deletes a habit.
    /// </summary>
    /// <param name="id">The habit unique identifier.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Habit deleted successfully.</response>
    /// <response code="404">Habit not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteHabit(string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

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

