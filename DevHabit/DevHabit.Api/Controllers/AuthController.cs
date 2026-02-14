using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Auth;
using DevHabit.Api.DTOs.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Controllers;
[Route("auth")]
[ApiController]
[AllowAnonymous]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    ApplicationDbContext appDbContext,
    ApplicationIdentityDbContext identityDbContext,
    TokenProvider tokenProvider,
    IOptions<JwtAuthOptions> options) : ControllerBase
{
    private readonly JwtAuthOptions _jwtAuthOptions = options.Value;

    [HttpPost("register")]
    public async Task<ActionResult<AccessTokenDto>> Register(RegisterUserDto registerUserDto)
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

        var tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email);
        AccessTokenDto accessToken = tokenProvider.Create(tokenRequest);

        await appDbContext.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessToken.RefreshToken,
            ExpireAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExiprationDays),
        };

        identityDbContext.RefreshTokens.Add(refreshToken);

        await identityDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(accessToken);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AccessTokenDto>> Login(LoginUserDto loginUserDto)
    {
        IdentityUser? identityUser = await userManager.FindByEmailAsync(loginUserDto.Email);

        if (identityUser is null || !await userManager.CheckPasswordAsync(identityUser, loginUserDto.Password))
        {
            return Unauthorized();
        }

        var tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email!);
        AccessTokenDto accessToken = tokenProvider.Create(tokenRequest);

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessToken.RefreshToken,
            ExpireAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExiprationDays),
        };

        identityDbContext.RefreshTokens.Add(refreshToken);

        await identityDbContext.SaveChangesAsync();

        return Ok(accessToken);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AccessTokenDto>> Refresh(RefreshTokenDto refreshTokenDto)
    {
        RefreshToken? refreshToken = await identityDbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken);

        if (refreshToken is null)
        {
            return Unauthorized();
        }

        if (refreshToken.ExpireAtUtc < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        var tokenRequest = new TokenRequest(refreshToken.User.Id, refreshToken.User.Email!);

        AccessTokenDto accessToken = tokenProvider.Create(tokenRequest);

        refreshToken.Token = accessToken.RefreshToken;
        refreshToken.ExpireAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExiprationDays);

        await identityDbContext.SaveChangesAsync();

        return Ok(accessToken);
    }
}
