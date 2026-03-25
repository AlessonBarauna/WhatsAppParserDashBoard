using FluentValidation;

namespace WhatsAppParser.Application.Features.Messages.Commands.ProcessFile;

public sealed class ProcessFileCommandValidator : AbstractValidator<ProcessFileCommand>
{
    public ProcessFileCommandValidator()
    {
        RuleFor(x => x.FileContent)
            .NotEmpty().WithMessage("File content cannot be empty.")
            .MaximumLength(5_000_000).WithMessage("File exceeds maximum allowed size of 5 MB.");

        RuleFor(x => x.SupplierName)
            .NotEmpty().WithMessage("Supplier name is required.")
            .MaximumLength(100).WithMessage("Supplier name must not exceed 100 characters.");
    }
}
