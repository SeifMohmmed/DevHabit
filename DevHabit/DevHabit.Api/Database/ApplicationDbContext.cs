using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Database;

/// <summary>
/// Primary EF Core database context.
/// Responsible for managing entity sets and database configuration.
/// </summary>
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Habit> Habits { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<HabitTag> HabitTags { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set default database schema
        modelBuilder.HasDefaultSchema(Schemas.Application);

        // Automatically apply IEntityTypeConfiguration implementations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
