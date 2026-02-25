using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace DevHabit.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Responsibilities:
/// - Start PostgreSQL container
/// - Start WireMock server
/// - Override application configuration
/// </summary>
public class DevHabitWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// PostgreSQL container used for tests.
    /// </summary>
    private readonly PostgreSqlContainer _postgreContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17.2")
        .WithDatabase("devhabit")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>
    /// WireMock server used to mock external HTTP APIs.
    /// </summary>
    private WireMockServer _wireMockServer;

    /// <summary>
    /// Gets WireMock instance.
    /// </summary>
    public WireMockServer GetWireMockServer() => _wireMockServer;

    /// <summary>
    /// Configure test host settings.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override DB connection
        builder.UseSetting("ConnectionStrings:Database", _postgreContainer.GetConnectionString());

        // Override external API base URL
        builder.UseSetting("GitHub:BaseUrl", _wireMockServer.Urls[0]);
        builder.UseSetting("Encryption:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        Quartz.Logging.LogContext.SetCurrentLogProvider(NullLoggerFactory.Instance);

    }

    /// <summary>
    /// Starts infrastructure before tests.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _postgreContainer.StartAsync();

        // Start WireMock AFTER container start
        _wireMockServer = WireMockServer.Start();
    }

    /// <summary>
    /// Stops infrastructure after tests.
    /// </summary>
    public new async Task DisposeAsync()
    {
        await _postgreContainer.StopAsync();
        _wireMockServer.Stop();
    }
}