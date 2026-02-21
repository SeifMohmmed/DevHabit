using System.Globalization;
using CsvHelper;
using DevHabit.Api.Database;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Jobs;

public sealed class ProcessEntryImportJob(
    ApplicationDbContext dbContext,
    ILogger<ProcessEntryImportJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string importJobId = context.MergedJobDataMap.GetString("importJobId")!;

        EntryImportJob? importJob = await dbContext.EntryImportJobs
            .FirstOrDefaultAsync(x => x.Id == importJobId, context.CancellationToken);

        if (importJob is null)
        {
            logger.LogError("Import job {ImportJobId} not found", importJobId);
            return;
        }

        try
        {
            importJob.Status = EntryImportStatus.Processing;
            await dbContext.SaveChangesAsync();

            using var memoryStream = new MemoryStream(importJob.FileContent.ToArray());
            using StreamReader streamReader = new(memoryStream);
            using CsvReader csv = new(streamReader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<CsvEntryRecord>().ToList();

            importJob.TotalRecords = records.Count;
            await dbContext.SaveChangesAsync();

            foreach (var record in records)
            {
                try
                {
                    Habit? habit = await dbContext.Habits
                        .FirstOrDefaultAsync(x => x.Id == record.HabitId && x.UserId == importJob.UserId);

                    if (habit is null)
                    {
                        throw new InvalidOperationException(
                            $"Habit with ID '{record.HabitId}' does not exists or does not belong to the user");
                    }

                    Entry entry = new()
                    {
                        Id = $"e_{Guid.CreateVersion7()}",
                        HabitId = record.HabitId,
                        UserId = importJob.UserId,
                        Value = habit.Target.Value,
                        Notes = record.Notes,
                        Source = EntrySource.FileImport,
                        Date = record.Date,
                        CreatedAtUtc = DateTime.UtcNow,
                    };

                    dbContext.Entries.Add(entry);
                    importJob.SuccessfulRecords++;
                }
                catch (Exception ex)
                {
                    importJob.FailedRecords++;
                    importJob.Errors.Add($"Error processing record: {ex.Message}");

                    if (importJob.Errors.Count >= 100)
                    {
                        importJob.Errors.Add("Too many errors, stopping error collection...");
                        break;
                    }
                }
                finally
                {
                    importJob.ProcessedRecords++;
                }

                if (importJob.ProcessedRecords % 100 == 0)
                {
                    await dbContext.SaveChangesAsync();
                }
            }

            importJob.Status = EntryImportStatus.Completed;
            importJob.CompletedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing import job {ImportJobId}", importJobId);

            importJob.Status = EntryImportStatus.Failed;
            importJob.Errors.Add($"Fatal error P{ex.Message}");
            importJob.CompletedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }
}
