using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Auth;
using DevHabit.Api.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DevHabit.Api.Controllers;
[Route("auth")]
[ApiController]
[AllowAnonymous]
public class AuthController(
    UserManager<IdentityUser> userManager,
    ApplicationDbContext appDbContext,
    ApplicationIdentityDbContext identityDbContext) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserDto registerUserDto)
    {
        using IDbContextTransaction transaction = await identityDbContext.Database.BeginTransactionAsync();
        appDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await appDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        //Create Identity User
        var identityUser = new IdentityUser
        {
            Email = registerUserDto.Email,
            UserName = registerUserDto.Email
        };

        IdentityResult identityResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!identityResult.Succeeded)
        {
            var extensions = new Dictionary<string, object?>
            {
                {
                    "errors",
                    identityResult.Errors.ToDictionary(e=>e.Code,e=>e.Description)
                }
            };

            return Problem(
                detail: "Unable to register user, please try again",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        //Creat App User
        var user = registerUserDto.ToEntity();
        user.IdentityId = identityUser.Id;

        appDbContext.Users.Add(user);

        await appDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(user.Id);
    }
}
