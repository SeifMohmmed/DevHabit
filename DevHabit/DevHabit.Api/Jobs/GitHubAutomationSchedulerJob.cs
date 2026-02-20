using DevHabit.Api.Database;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Jobs;

// Prevents Quartz from running multiple instances of this job at the same time
[DisallowConcurrentExecution]
public sealed class GitHubAutomationSchedulerJob(
    ApplicationDbContext dbContext,
    ILogger<GitHubAutomationSchedulerJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            // Log start of scheduler execution
            logger.LogInformation("Starting GitHub automation scheduler job");

            // Fetch all active habits that are configured to use GitHub automation
            List<Habit> habitsToProcess = await dbContext.Habits
                .Where(x => x.AutomationSource == AutomationSource.GitHub && !x.IsArchived)
                .ToListAsync(context.CancellationToken);

            logger.LogInformation("Found {Count} habits with GitHub automation", habitsToProcess.Count);

            // Loop through each habit and schedule a processor job
            foreach (var habit in habitsToProcess)
            {
                // Create trigger to run immediately
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity($"github-habit-{habit.Id}", "github-habits")
                    .StartNow()
                    .Build();

                // Create job detail and pass habitId via JobDataMap
                IJobDetail jobDetail = JobBuilder.Create<GitHubHabitProcessorJob>()
                    .WithIdentity($"github-habit-{habit.Id}", "github-habits")
                    .UsingJobData("habitId", habit.Id)
                    .Build();

                // Schedule the processor job
                await context.Scheduler.ScheduleJob(jobDetail, trigger);

                logger.LogInformation("Scheduled processor job for habit {HabitId}", habit.Id);
            }

            logger.LogInformation("Completed GitHub automation scheduler job");
        }
        catch (Exception ex)
        {
            // Log and rethrow so Quartz can handle retry logic
            logger.LogError(ex, "Error scheduling GitHub automation scheduler job");
            throw;
        }
    }
}
