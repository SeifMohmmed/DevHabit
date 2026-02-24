using DevHabit.Api.DTOs.Auth;
using DevHabit.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace DevHabit.IntegrationTests.Tests;

public sealed class AuthenticationTests(DevHabitWebAppFactory factory) : IntegrationTestFixture(factory)
{
    [Fact]
    public async Task Register_ShouldSucceed_WithValidParameters()
    {
        //Arrange
        var dto = new RegisterUserDto
        {
            Name = "register@test.com",
            Email = "register@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        HttpClient client = CreateClient();

        //Act
        HttpResponseMessage response = await client.PostAsJsonAsync(Routes.Auth.Register, dto);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturnAccessToken_WithValidParameters()
    {
        //Arrange
        var dto = new RegisterUserDto
        {
            Name = "register@test.com",
            Email = "register@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        HttpClient client = CreateClient();

        //Act
        HttpResponseMessage response = await client.PostAsJsonAsync("auth/register", dto);
        response.EnsureSuccessStatusCode();

        //Assert
        AccessTokenDto? accessTokenDto = await response.Content.ReadFromJsonAsync<AccessTokenDto>();
        Assert.NotNull(accessTokenDto);
    }
}
