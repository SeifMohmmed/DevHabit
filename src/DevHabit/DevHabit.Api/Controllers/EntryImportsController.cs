using Asp.Versioning;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.EntryImports;
using DevHabit.Api.Entities;
using DevHabit.Api.Jobs;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("entries/imports")]
[ApiVersion(1.0)]
[Authorize(Roles = Roles.Member)]
[Produces(
    CustomMediaTypeNames.Application.HateoasJson,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.JsonV2)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class EntryImportsController(
    ApplicationDbContext dbContext,
    UserContext userContext,
    LinkService linkService,
    ISchedulerFactory schedulerFactory) : ControllerBase
{
    /// <summary>
    /// Gets paginated entry import jobs for the authenticated user.
    /// </summary>
    /// <param name="acceptHeaderDto">Accept header configuration.</param>
    /// <param name="page">Page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paginated list of import jobs.</returns>
    /// <response code="200">Import jobs retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResult<EntryImportJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImportJobs(
        [FromHeader] AcceptHeaderDto acceptHeaderDto,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (userId is null)
        {
            return Unauthorized();
        }

        IQueryable<EntryImportJob> query = dbContext.EntryImportJobs
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAtUtc);

        int totalCount = await query.CountAsync();

        List<EntryImportJobDto> importJobDtos = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(EntryImportQueries.ProjectToDto())
            .ToListAsync();

        if (acceptHeaderDto.IncludeLinks)
        {
            foreach (EntryImportJobDto dto in importJobDtos)
            {
                dto.Links = CreateLinksForImportJob(dto.Id);
            }
        }

        var result = new PaginationResult<EntryImportJobDto>
        {
            Items = importJobDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        if (acceptHeaderDto.IncludeLinks)
        {
            result.Links = CreateLinksForImportJobs(page, pageSize, result.HasPreviousPage, result.HasNextPage);
        }


        return Ok(result);
    }

    /// <summary>
    /// Gets an import job by id.
    /// </summary>
    /// <param name="id">The import job unique identifier.</param>
    /// <param name="acceptHeaderDto">Accept header configuration.</param>
    /// <returns>The import job details.</returns>
    /// <response code="200">Import job retrieved successfully.</response>
    /// <response code="404">Import job not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EntryImportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImportJob(
        string id,
       [FromHeader] AcceptHeaderDto acceptHeaderDto)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (userId is null)
        {
            return Unauthorized();
        }

        EntryImportJobDto? importJob = await dbContext.EntryImportJobs
            .Where(j => j.Id == id && j.UserId == userId)
            .Select(EntryImportQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        if (importJob is null)
        {
            return NotFound();
        }

        if (acceptHeaderDto.IncludeLinks)
        {
            importJob.Links = CreateLinksForImportJob(id);
        }

        return Ok(importJob);
    }

    /// <summary>
    /// Creates a new entry import job.
    /// </summary>
    /// <param name="createImportJob">Import job creation data.</param>
    /// <param name="acceptHeaderDto">Accept header configuration.</param>
    /// <param name="validator">Import job validator.</param>
    /// <returns>The created import job.</returns>
    /// <response code="201">Import job created successfully.</response>
    /// <response code="400">Invalid import request.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EntryImportJobDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateImportJob(
     [FromForm] CreateEntryImportJobDto createImportJob,
     [FromHeader] AcceptHeaderDto acceptHeaderDto,
        IValidator<CreateEntryImportJobDto> validator)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (userId is null)
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createImportJob);

        using MemoryStream memoryStream = new();
        await createImportJob.File.CopyToAsync(memoryStream);

        var importJob = createImportJob.ToEntity(userId, memoryStream.ToArray());

        dbContext.EntryImportJobs.Add(importJob);
        await dbContext.SaveChangesAsync();

        IScheduler scheduler = await schedulerFactory.GetScheduler();

        IJobDetail jobDetail = JobBuilder.Create<ProcessEntryImportJob>()
            .WithIdentity($"process-entry-import-{importJob.Id}")
            .UsingJobData("importJobId", importJob.Id)
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity($"process-entry-import-trigger-{importJob.Id}")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);

        EntryImportJobDto importJobDto = importJob.ToDto();

        if (acceptHeaderDto.IncludeLinks)
        {
            importJobDto.Links = CreateLinksForImportJob(importJob.Id);
        }

        return CreatedAtAction(nameof(GetImportJobs), new { id = importJobDto.Id }, importJobDto);
    }

    private List<LinkDto> CreateLinksForImportJob(string id)
    {
        return
        [
            linkService.Create(nameof(GetImportJob), "self", HttpMethods.Get, new { id }),
        ];

    }
    private List<LinkDto> CreateLinksForImportJobs(
        int page,
        int pageSize,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetImportJobs), "self", HttpMethods.Get, new
            {
                page,
                page_size = pageSize,
            }),
            linkService.Create(nameof(CreateImportJob), "create", HttpMethods.Post),
        ];

        if (hasPreviousPage)
        {
            links.Add(linkService.Create(nameof(GetImportJobs), "previous-page", HttpMethods.Get, new
            {
                page = page - 1,
                page_size = pageSize,
            }));
        }

        if (hasNextPage)
        {
            links.Add(linkService.Create(nameof(GetImportJobs), "next-page", HttpMethods.Get, new
            {
                page = page + 1,
                page_size = pageSize,
            }));
        }

        return links;
    }
}
