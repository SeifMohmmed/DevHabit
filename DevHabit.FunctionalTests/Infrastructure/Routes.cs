namespace DevHabit.FunctionalTests.Infrastructure;
public static class Routes
{
    public static class AuthRoutes
    {
        public const string Register = "auth/register";
        public const string Login = $"auth/login";
        public const string Refresh = "auth/refresh";
    }

    public static class EntryRoutes
    {
        public const string GetAll = "entries";
        public const string GetAllCursor = $"entries/cursor";
        public const string Get = "entries";
        public const string Create = "entries";
        public const string CreateBatch = "entries/batch";
        public const string Update = "entries";
        public static string Archive(string entryId) => $"entries/{entryId}/archive";
        public static string UnArchive(string entryId) => $"entries/{entryId}/un-archive";
        public const string Delete = $"entries";
        public const string Stats = $"entries/stats";
    }

    public static class EntryImportJobRoutes
    {
        public const string GetAll = $"entries/imports";
        public const string Get = $"entries/imports";
        public const string Create = $"entries/imports";
    }

    public static class GitHubRoutes
    {
        public const string GetUserProfile = $"github/profile";
        public const string GetUserEvents = $"github/events";
        public const string StorePersonalAccessToken = $"github/personal-access-token";
        public const string RevokePersonalAccessToken = $"github/personal-access-token";
    }

    public static class HabitRoutes
    {
        public const string GetAll = $"habits";
        public const string Get = $"habits";
        public const string Create = $"habits";
        public const string Patch = $"habits";
        public const string Update = $"habits";
        public const string Delete = $"habits";
    }

    public static class HabitTagsRoutes
    {
        public static string Upsert(string habitId) => $"habits/{habitId}/tags";
        public static string Delete(string habitId, string tagId) => $"habits/{habitId}/tags/{tagId}";
    }

    public static class TagRoutes
    {
        public const string GetAll = $"tags";
        public const string Get = $"tags";
        public const string Create = $"tags";
        public const string Update = $"tags";
        public const string Delete = $"tags";
    }

    public static class UserRoutes
    {
        public const string CurrentUser = $"users/me";
        public const string UpdateProfile = $"users/me/profile";
    }
}