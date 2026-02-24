using DevHabit.Api.DTOs.Github;
using DevHabit.IntegrationTests.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace DevHabit.IntegrationTests.Tests;
public sealed class GitHubTests(DevHabitWebAppFactory factory) : IntegrationTestFixture(factory)
{
    private const string TestAccessToken = "gho_test123456789";

    private static readonly GitHubUserProfileDto User = new()
    {
        Login = "testuser",
        Name = "Test User",
        AvatarUrl = new Uri("https://github.com/testuser.png"),
        Bio = "Test bio",
        PublicRepos = 30,
        Followers = 20,
        Following = 30,
    };

    private static readonly GitHubEventDto TestEvent = new GitHubEventDto(
    Id: "1234555",
    Type: "PushEvent",

    Actor: new GitHubEventActorDto(
        Id: 1,
        Login: "testUser",
        DisplayLogin: "testuser",
        GravatarId: "123dfafs",
        Url: new Uri("https://api.github.com/users/testuser"),
        AvatarUrl: new Uri("https://github.com/testuser.png")
    ),

    Repo: new GitHubEventRepoDto(
        Id: 1,
        Name: "testuser/repo",
        Url: new Uri("https://github.com/testuser/repo")
    ),

    Payload: new GitHubEventPayloadDto(
        Action: "test-action",
        Commits:
        [
            new Commit(
                Sha: "abc123",
                Author: new Author(
                    Name: "Test User",
                    Email: "test@test.com"
                ),
                Message: "test Commit",
                Distinct: true,
                Url: new Uri("https://github.com/testuser/repo/commit/abc123")
            )
        ]
    ),

    Public: true,
    CreatedAt: DateTime.UtcNow
);

    [Fact]
    public async Task GetProfile_ShouldReturnUserProfile_WhenAccessTokenIsValid()
    {
        //Arrange
        WireMockServer
            .Given(Request.Create()
            .WithPath("/user")
            .WithHeader("Authorization", $"Bearer {TestAccessToken}")
            .UsingGet())
            .RespondWith(Response.Create()
            .WithStatusCode(HttpStatusCode.OK)
            .WithHeader("Content-Type", MediaTypeNames.Application.Json)
            .WithBodyAsJson(User));

        HttpClient client = await CreateAuthenticatedClientAsync();

        var dto = new StoreGithubAccessTokenDto
        {
            AccessToken = TestAccessToken,
            ExpiresInDays = 30
        };

        await client.PutAsJsonAsync(Routes.GitHub.StoreAccessToken, dto);

        //Act 
        HttpResponseMessage response = await client.GetAsync(Routes.GitHub.GetProfile);
        response.EnsureSuccessStatusCode();

        //Assert
        GitHubUserProfileDto? profile = JsonConvert.DeserializeObject<GitHubUserProfileDto>(
            await response.Content.ReadAsStringAsync());

        Assert.NotNull(profile);
        Assert.Equivalent(User, profile);
    }
}
