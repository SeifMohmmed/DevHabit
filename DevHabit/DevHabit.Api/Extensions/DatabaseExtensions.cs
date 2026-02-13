using DevHabit.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Extensions;

/// <summary>
/// Contains extension methods related to database initialization.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending Entity Framework Core migrations automatically at application startup.
    /// Ensures database schema is up to date with current models.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        // Create a scoped service provider
        // Required because DbContext is registered as scoped service
        using IServiceScope scope = app.Services.CreateScope();

        // Resolve ApplicationDbContext from DI container
        await using ApplicationDbContext applicationDbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Resolve ApplicationDbContext from DI container
        await using ApplicationIdentityDbContext identityDbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();

        try
        {
            // Apply all pending migrations
            await applicationDbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Application database migrations applied successfully.");

            await identityDbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Identity database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            // Log migration failure
            app.Logger.LogError(ex, "An error occurred while applying database migrations.");

            // Re-throw exception so application fails fast
            throw;
        }
    }
}
