namespace DevHabit.IntegrationTests.Infrastructure;

/// <summary>
/// Centralized API route definitions used in integration tests.
/// Helps avoid magic strings and keeps endpoints consistent.
/// </summary>
public static class Routes
{
    /// <summary>
    /// Authentication endpoints.
    /// </summary>
    public static class Auth
    {
        public const string Register = "auth/register";
        public const string Login = "auth/login";
    }

    /// <summary>
    /// Habit endpoints.
    /// </summary>
    public static class Habits
    {
        public const string Create = "habits";
        public const string GetAll = "habits";
    }
    /// <summary>
    /// Github endpoints.
    /// </summary>
    public static class GitHub
    {
        public const string StoreAccessToken = "github/personal-access-token";
        public const string GetProfile = "github/profile";
        public const string GetEvents = "github/events";
    }
}
