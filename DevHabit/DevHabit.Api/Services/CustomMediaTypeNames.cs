namespace DevHabit.Api.Services;

/// <summary>
/// Contains custom media type names used by the API.
/// These media types allow clients to request specific response formats.
/// </summary>
public static class CustomMediaTypeNames
{
    public static class Application
    {
        /// <summary>
        /// Custom media type for responses that include HATEOAS links.
        /// Used in content negotiation.
        /// </summary>
        public const string HateoasJson = "application/vnd.dev-habit.hateoas+json";
    }
}
