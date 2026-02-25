using System.Linq.Expressions;
using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.EntryImports;


public static class EntryImportQueries
{
    public static Expression<Func<EntryImportJob, EntryImportJobDto>> ProjectToDto()
    {
        return entry => new EntryImportJobDto
        {
            Id = entry.Id,
            UserId = entry.UserId,
            Status = entry.Status,
            FileName = entry.FileName,
            CreatedAtUtc = entry.CreatedAtUtc,
        };
    }
}
