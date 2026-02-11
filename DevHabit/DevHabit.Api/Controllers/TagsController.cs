using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Tags;
using DevHabit.Api.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;
[Route("tags")]
[ApiController]
public class TagsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TagsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<TagsCollectionDto>> GetTags()
    {
        List<TagDto> tags = await _context
            .Tags
            .Select(TagQueries.ProjectToDto())
            .ToListAsync();

        var tagsCollectionDto = new TagsCollectionDto
        {
            Data = tags
        };

        return Ok(tagsCollectionDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(string id)
    {
        TagDto tag = await _context
             .Tags
             .Where(t => t.Id == id)
             .Select(TagQueries.ProjectToDto())
             .AsNoTracking()
             .FirstOrDefaultAsync();

        return tag is null ? NotFound() : Ok(tag);
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

        if (await _context.Tags.AnyAsync(t => t.Name == tag.Name))
        {
            return Problem(detail: $"The tag '{tag.Name}' already exists",
                           statusCode: StatusCodes.Status409Conflict);
        }
        _context.Tags.Add(tag);

        await _context.SaveChangesAsync();

        TagDto tagDto = tag.ToDto();

        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTag(string id, UpdateTagDto updateTagDto)
    {
        Tag? tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            return NotFound();
        }

        tag.UpdateFromDto(updateTagDto);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(string id)
    {
        Tag? tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id);

        if (tag is null)
        {
            return NotFound();
        }

        _context.Tags.Remove(tag);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
