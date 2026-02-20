namespace DevHabit.Api.Settings;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "DevHabitCorsPolicy";

    public required string[] AllowedOrigins { get; init; }
}
