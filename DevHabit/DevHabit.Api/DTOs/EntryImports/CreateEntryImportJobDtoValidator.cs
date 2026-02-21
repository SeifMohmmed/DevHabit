using FluentValidation;

namespace DevHabit.Api.DTOs.EntryImports;

public sealed class CreateEntryImportJobDtoValidator : AbstractValidator<CreateEntryImportJobDto>
{
    private const int MaxFileSizeInMegabytes = 10;
    private const int MaxFileSizeInBytes = MaxFileSizeInMegabytes * 1024 * 1024;
    public CreateEntryImportJobDtoValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("File is required");

        RuleFor(x => x.File.Length)
            .LessThan(MaxFileSizeInBytes)
            .WithMessage($"File size must be less than {MaxFileSizeInMegabytes}MB");

        RuleFor(x => x.File.FileName)
            .Must(filename => filename.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("File Must be a CSV file");

    }
}
