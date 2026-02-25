namespace DevHabit.Api.Settings;

public sealed class GitHubAutomationOptions
{
    public const string SectionName = "Jobs";
    public int ScanIntervalInMinutes { get; set; }
}
