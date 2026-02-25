using DevHabit.Api.Common.Auth;
using DevHabit.Api.Database;
using Microsoft.AspNetCore.Identity;
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

    // Extension method to seed initial roles when the application starts
    public static async Task SeedInitialDataAsync(this WebApplication app)
    {
        // Create a scoped service provider to resolve scoped services like RoleManager
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

        // Resolve ASP.NET Identity RoleManager to manage roles
        RoleManager<IdentityRole> roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            // Check if Admin role exists, if not create it
            if (!await roleManager.RoleExistsAsync(Roles.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            }

            // Check if Member role exists, if not create it
            if (!await roleManager.RoleExistsAsync(Roles.Member))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Member));
            }

            // Log success message
            app.Logger.LogInformation("Roles created successfully");
        }
        catch (Exception ex)
        {
            // Log error if something goes wrong during seeding
            app.Logger.LogError(ex, "An error occurred while seeding initial data");
            throw;
        }
    }
}
