using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;
[Route("users")]
[ApiController]
internal sealed class UsersController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(string id)
    {
        UserDto? user = await context.Users
            .Where(u => u.Id == id)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }

}
