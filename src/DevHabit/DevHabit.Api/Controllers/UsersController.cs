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
    /// Gets a user by id (Admin only).
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <returns>The user details.</returns>
    /// <response code="200">User retrieved successfully.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Gets the currently authenticated user.
    /// </summary>
    /// <returns>The current user details.</returns>
    /// <response code="200">User retrieved successfully.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
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
