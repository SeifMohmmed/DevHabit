using System.Net.Mime;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.DTOs.Tags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;
[ApiController]
[Route("tags")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.JsonV2,
    CustomMediaTypeNames.Application.HateoasJsonV1,
    CustomMediaTypeNames.Application.HateoasJsonV2)]
[Authorize]
public sealed class TagsController(ApplicationDbContext context, LinkService linkService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TagsCollectionDto>> GetTags([FromHeader] AcceptHeaderDto acceptHeader)
    {
        List<TagDto> tags = await context
            .Tags
            .Select(TagQueries.ProjectToDto())
            .ToListAsync();

        var tagsCollectionDto = new TagsCollectionDto
        {
            Items = tags
        };

        if (acceptHeader.IncludeLinks)
        {
            tagsCollectionDto.Links = CreateLinksForTags();
        }

        return Ok(tagsCollectionDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(string id, [FromHeader] AcceptHeaderDto acceptHeader)
    {
        TagDto? tag = await context
             .Tags
             .Where(t => t.Id == id)
             .Select(TagQueries.ProjectToDto())
             .AsNoTracking()
             .FirstOrDefaultAsync();

        if (tag is null)
        {
            return NotFound();
        }

        if (acceptHeader.IncludeLinks)
        {
            tag.Links = CreateLinksForTag(id);
        }

        return Ok(tag);
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag(
        CreateTagDto createTagDto,
        IValidator<CreateTagDto> validator,
        ProblemDetailsFactory problemDetailsFactory)
    {
        //1) Missing type Property & Trace Identifier
        //ValidationResult validationResult = await validator.ValidateAsync(createTagDto);

        //if (!validationResult.IsValid)
        //{
        //    return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        //}

        //2) Proper Problem Details
        ValidationResult validationResult = await validator.ValidateAsync(createTagDto);

        if (!validationResult.IsValid)
        {
            ProblemDetails problem = problemDetailsFactory.CreateProblemDetails(HttpContext,
                StatusCodes.Status400BadRequest);
            problem.Extensions.Add("errors", validationResult.ToDictionary());
            return BadRequest(problem);
        }


        Tag tag = createTagDto.ToEntity();

        if (await context.Tags.AnyAsync(t => t.Name == tag.Name))
        {
            return Problem(detail: $"The tag '{tag.Name}' already exists",
                           statusCode: StatusCodes.Status409Conflict);
        }
        context.Tags.Add(tag);

        await context.SaveChangesAsync();

        TagDto tagDto = tag.ToDto();

        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTag(string id, UpdateTagDto updateTagDto)
    {
        Tag? tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            return NotFound();
        }

        tag.UpdateFromDto(updateTagDto);

        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(string id)
    {
        Tag? tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            return NotFound();
        }

        context.Tags.Remove(tag);

        await context.SaveChangesAsync();

        return NoContent();
    }

    //For Collection Resource
    private List<LinkDto> CreateLinksForTags()
    {
        var query = HttpContext.Request.Query;

        int page = int.TryParse(query["page"], out var p) ? p : 1;
        int pageSize = int.TryParse(query["pageSize"], out var ps) ? ps : 10;

        string? fields = query["fields"];
        string? search = query["q"];
        string? sort = query["sort"];
        string? type = query["type"];
        string? status = query["status"];

        bool hasNextPage = HttpContext.Items["HasNextPage"] as bool? ?? false;
        bool hasPreviousPage = HttpContext.Items["HasPreviousPage"] as bool? ?? false;

        List<LinkDto> links =
        [
            linkService.Create(nameof(GetTags), "self", HttpMethods.Get, new
        {
            page,
            pageSize,
            fields,
            q = search,
            sort,
            type,
            status
        }),
        linkService.Create(nameof(CreateTag), "create", HttpMethods.Post)
        ];

        if (hasNextPage)
        {
            links.Add(linkService.Create(nameof(GetTags), "next-page", HttpMethods.Get, new
            {
                page = page + 1,
                pageSize,
                fields,
                q = search,
                sort,
                type,
                status
            }));
        }

        if (hasPreviousPage)
        {
            links.Add(linkService.Create(nameof(GetTags), "previous-page", HttpMethods.Get, new
            {
                page = page - 1,
                pageSize,
                fields,
                q = search,
                sort,
                type,
                status
            }));
        }

        return links;
    }
    //For Single Resource
    private List<LinkDto> CreateLinksForTag(string id)
    {
        List<LinkDto> links =
           [
                linkService.Create(nameof(GetTag),"self",HttpMethods.Get),
                linkService.Create(nameof(GetTag),"update",HttpMethods.Put),
                linkService.Create(nameof(GetTag),"partial-update",HttpMethods.Patch),
                linkService.Create(nameof(GetTag),"delete",HttpMethods.Delete),
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
