using DevHabit.Api.Common.Auth;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Users;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;
[Route("users")]
[ApiController]
[Authorize(Roles = Roles.Member)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class UsersController(
    ApplicationDbContext context,
    UserContext userContext) : ControllerBase
{
    /// <summary>
    /// Gets User By Id (Admin Only)
    /// </summary>
    /// <param name="id">the user's unique identifier</param>
    /// <returns>The user details</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUserById(string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (id != userId)
        {
            return Forbid();
        }

        UserDto? user = await context.Users
            .Where(u => u.Id == id)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        UserDto? user = await context.Users
            .Where(u => u.Id == userId)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }
}
