using System.Dynamic;
using System.Net.Mime;
using Asp.Versioning;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Entires;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;
[Route("entries")]
[ApiController]
[Authorize(Roles = Roles.Member)]
[ApiVersion(1.0)]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.HateoasJson,
    CustomMediaTypeNames.Application.HateoasJsonV1)]
public class EntriesController(
    ApplicationDbContext context,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEntries(
       [FromQuery] EntriesParameters query,
       SortMappingProvider sortMappingProvider,
       DataShapingService dataShapingService)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!sortMappingProvider.ValidateMappings<EntryDto, Entry>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parmeter isn't valid: `{query.Sort}`");
        }


        if (!dataShapingService.Validate<EntryDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shapping aren't valid: `{query.Fields}`");
        }

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<EntryDto, Entry>();

        IQueryable<Entry> entriesQuery = context.Entries
            .Where(e => e.UserId == userId)
            .Where(e => query.HabitId == null || e.HabitId == query.HabitId)
            .Where(e => query.FromDate == null || e.Date >= query.FromDate)
            .Where(e => query.ToDate == null || e.Date <= query.ToDate)
            .Where(e => query.Source == null || e.Source == query.Source)
            .Where(e => query.IsArchived == null || e.IsArchived == query.IsArchived);

        int totalCount = await entriesQuery.CountAsync();

        List<EntryDto> entries = await entriesQuery
            .ApplySort(query.Sort, sortMappings)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(EntryQueries.ProjectToDto())
            .ToListAsync();

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(
                entries,
                query.Fields,
                query.IncludeLinks ? e => CreateLinksForEntry(e.Id, query.Fields, e.IsArchived)
                : null),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        if (query.IncludeLinks)
        {
            paginationResult.Links = CreateLinksForEntries(
                query,
                paginationResult.HasNextPage,
                paginationResult.HasPreviousPage);
        }

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEntry(
        string id,
        [FromQuery] EntryParameters query,
        DataShapingService dataShapingService)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShapingService.Validate<EntryDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shapping aren't valid: `{query.Fields}`");
        }

        EntryDto? entry = await context.Entries
            .Where(e => e.Id == id && e.UserId == userId)
            .Select(EntryQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        if (entry is null)
        {
            return NotFound();
        }

        ExpandoObject shapedEntryDto = dataShapingService.ShapeData(entry, query.Fields);

        if (query.IncludeLinks)
        {
            ((IDictionary<string, object?>)shapedEntryDto)[nameof(ILinksResponse.Links)] =
                CreateLinksForEntry(id, query.Fields, entry.IsArchived);
        }

        return Ok(shapedEntryDto);
    }

    [HttpPost]
    public async Task<ActionResult<EntryDto>> CreateEntry(
        CreateEntryDto createEntryDto,
        [FromHeader] AcceptHeaderDto acceptHeader,
       [FromServices] IValidator<CreateEntryDto> validator)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createEntryDto);

        Habit? habit = await context.Habits
            .FirstOrDefaultAsync(h => h.Id == createEntryDto.HabitId && h.UserId == userId);

        if (habit is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"Habit with ID: `{createEntryDto.HabitId}` does not exist.");
        }

        Entry entry = createEntryDto.ToEntity(userId);

        context.Entries.Add(entry);
        await context.SaveChangesAsync();

        EntryDto entryDto = entry.ToDto();

        if (acceptHeader.IncludeLinks)
        {
            entryDto.Links =
                CreateLinksForEntry(entry.Id, null, entry.IsArchived);
        }

        return CreatedAtAction(nameof(GetEntry), new { id = entryDto.Id }, entryDto);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<List<EntryDto>>> CreateEntryBatch(
        CreateEntryBatchDto createEntryBatchDto,
        [FromHeader] AcceptHeaderDto acceptHeaderDto,
      [FromServices] IValidator<CreateEntryBatchDto> validator)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createEntryBatchDto);

        var habitIds = createEntryBatchDto.Entries
            .Select(x => x.HabitId)
            .ToHashSet();

        List<Habit> existingHabits = await context.Habits
            .Where(x => habitIds.Contains(x.Id) && x.UserId == userId)
            .ToListAsync();

        if (existingHabits.Count != habitIds.Count)
        {
            return Problem(
                detail: "One or more habit IDs are invalid",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var entries = createEntryBatchDto.Entries
            .Select(x => x.ToEntity(userId))
            .ToList();

        context.Entries.AddRange(entries);

        await context.SaveChangesAsync();

        var entryDtos = entries.Select(x => x.ToDto()).ToList();

        if (acceptHeaderDto.IncludeLinks)
        {
            foreach (EntryDto entryDto in entryDtos)
            {
                entryDto.Links = CreateLinksForEntry(entryDto.Id, null, entryDto.IsArchived);
            }
        }
        return CreatedAtAction(nameof(GetEntries), entryDtos);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEntry(
            string id,
            UpdateEntryDto updateEntryDto,
           [FromServices] IValidator<UpdateEntryDto> validator)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(updateEntryDto);

        Entry? entry = await context.Entries
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        entry.UpdateFromDto(updateEntryDto);

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/archive")]
    public async Task<IActionResult> ArchiveEntry(string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Entry? entry = await context.Entries
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        entry.IsArchived = true;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/un-archive")]
    public async Task<IActionResult> UnArchiveEntry(string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Entry? entry = await context.Entries
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        entry.IsArchived = false;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntry(string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Entry? entry = await context.Entries
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        context.Entries.Remove(entry);

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var entries = await context.Entries
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Date)
            .Select(x => new { x.Date })
            .ToListAsync();

        if (!entries.Any())
        {
            return Ok(new
            {
                DailyStats = Enumerable.Empty<string>(),
                TotalEntries = 0,
                CurrentStreak = 0,
                LongestStreak = 0,
            });
        }

        var dailyStats = entries
            .GroupBy(x => x.Date)
            .Select(x => new
            {
                Date = x.Key,
                Count = x.Count(),
            })
            .OrderByDescending(x => x.Date)
            .ToList();

        // int totalEntries = entries.Count;

        DateTime today = DateTime.UtcNow;
        var dates = entries
            .Select(x => x.Date)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        int currentStreak = 0;
        int longestStreak = 0;
        int currentCount = 0;


        for (int i = dates.Count - 1; i >= 0; i--)
        {
            if (i == dates.Count - 1)
            {
                if (dates[i] == today)
                {
                    currentStreak = 1;
                }
                else
                {
                    break;
                }
            }
            else if (dates[i].AddDays(1) == dates[i + 1])
            {
                currentCount++;
            }
            else
            {
                break;
            }
        }

        for (int i = 0; i < dates.Count; i++)
        {
            if (i == 0 || dates[i] == dates[i - 1].AddDays(1))
            {
                currentCount++;
                longestStreak = Math.Max(longestStreak, currentCount);
            }
            else
            {
                currentCount = 1;
            }
        }

        return Ok(new
        {
            DailyStats = dailyStats,
            TotalEntries = entries.Count,
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
        });
    }


    private List<LinkDto> CreateLinksForEntry(string id, string? fields = null, bool isArchived = false)
    {
        List<LinkDto> links =
            [
        linkService.Create(nameof(GetEntry), "self", HttpMethods.Get, new { id, fields }),
        linkService.Create(nameof(UpdateEntry), "update", HttpMethods.Put, new { id }),
        linkService.Create(nameof(DeleteEntry), "delete", HttpMethods.Delete, new { id }),

        isArchived
            ? linkService.Create(nameof(UnArchiveEntry), "un-archive", HttpMethods.Put, new { id })
            : linkService.Create(nameof(ArchiveEntry), "archive", HttpMethods.Put, new { id }),
        ];

        return links;
    }

    private List<LinkDto> CreateLinksForEntries(
        EntriesParameters parameters,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetEntries), "self", HttpMethods.Get, new
            {
                page = parameters.Page,
                page_size = parameters.PageSize,
                fields = parameters.Fields,
                sort = parameters.Sort,
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
            }),
            linkService.Create(nameof(GetStats), "stats", HttpMethods.Get),
            linkService.Create(nameof(CreateEntry), "create", HttpMethods.Post),
            linkService.Create(nameof(CreateEntryBatch), "create-batch", HttpMethods.Post),
        ];

        if (hasPreviousPage)
        {
            links.Add(linkService.Create(nameof(GetEntries), "previous-page", HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                sort = parameters.Sort,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                page = parameters.Page - 1,
                page_size = parameters.PageSize,
            }));
        }

        if (hasNextPage)
        {
            links.Add(linkService.Create(nameof(GetEntries), "next-page", HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                sort = parameters.Sort,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                page = parameters.Page + 1,
                page_size = parameters.PageSize,
            }));
        }

        return links;
    }
}
