using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WireMock.Server;

namespace DevHabit.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests.
/// Provides helper utilities such as:
/// - Creating HttpClient instances
/// - Creating authenticated clients
/// - Cleaning database state between tests
/// - Accessing WireMock server for external API mocking
/// </summary>
/// <param name="factory">Shared WebApplicationFactory instance.</param>
public abstract class IntegrationTestFixture(DevHabitWebAppFactory factory) : IClassFixture<DevHabitWebAppFactory>
{
    /// <summary>
    /// Cached authenticated client to avoid repeated login calls.
    /// </summary>
    private HttpClient? _authorizedClient;

    /// <summary>
    /// Creates a new HttpClient targeting the test server.
    /// </summary>
    public HttpClient CreateClient() => factory.CreateClient();

    /// <summary>
    /// Gets the WireMock server instance used to mock external APIs.
    /// </summary>
    public WireMockServer WireMockServer => factory.GetWireMockServer();

    /// <summary>
    /// Cleans database by truncating all application and identity tables.
    /// Should be called before each test to ensure isolation.
    /// </summary>
    protected async Task CleanUpDatabaseAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        string? connectionString = configuration.GetConnectionString("Database");
        if (connectionString is null)
        {
            throw new InvalidOperationException("Database connection string not found in configuration");
        }

        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        // Execute PostgreSQL anonymous DO block to truncate tables
        await using NpgsqlCommand command = new(@"
        DO $$
        BEGIN
            -- Application tables
            TRUNCATE TABLE dev_habit.entries CASCADE;
            TRUNCATE TABLE dev_habit.entry_import_jobs CASCADE;
            TRUNCATE TABLE dev_habit.tags CASCADE;
            TRUNCATE TABLE dev_habit.habits CASCADE;
            TRUNCATE TABLE dev_habit.users CASCADE;

            -- Identity tables
            TRUNCATE TABLE identity.asp_net_users CASCADE;
            TRUNCATE TABLE identity.refresh_tokens CASCADE;
        END $$;
    ", connection);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates an authenticated HttpClient.
    /// 
    /// Workflow:
    /// 1️) Check if test user exists  
    /// 2️) Register user if missing  
    /// 3️) Login to obtain JWT  
    /// 4️) Set Authorization header  
    /// </summary>
    /// <param name="email">Test user email.</param>
    /// <param name="password">Test user password.</param>
    /// <returns>Authenticated HttpClient.</returns>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "test@test.com",
        string password = "Test123!")
    {
        if (_authorizedClient is not null)
        {
            return _authorizedClient;
        }

        HttpClient client = CreateClient();

        // Check if user exists in database
        bool userExists;
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            userExists = await dbContext.Users.AnyAsync(u => u.Email == email);
        }

        // Register if missing
        if (!userExists)
        {
            HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
                Routes.Auth.Register,
                new RegisterUserDto
                {
                    Email = email,
                    Name = email,
                    Password = password,
                    ConfirmPassword = password
                });

            registerResponse.EnsureSuccessStatusCode();
        }

        // Login
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            Routes.Auth.Login,
            new LoginUserDto
            {
                Email = email,
                Password = password
            });

        loginResponse.EnsureSuccessStatusCode();

        AccessTokenDto? loginResult = await loginResponse.Content.ReadFromJsonAsync<AccessTokenDto>();

        if (loginResult?.AccessToken is null)
        {
            throw new InvalidOperationException("Failed to get authentication token");
        }

        // Attach JWT
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        _authorizedClient = client;

        return client;
    }
}