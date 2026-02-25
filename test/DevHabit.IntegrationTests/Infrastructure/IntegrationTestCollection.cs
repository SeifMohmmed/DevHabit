namespace DevHabit.IntegrationTests.Infrastructure;

/// <summary>
/// Defines a test collection to share the DevHabitWebAppFactory
/// across multiple test classes.
/// 
/// This ensures the same container and application instance
/// is reused to improve performance and maintain consistency.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<DevHabitWebAppFactory>;