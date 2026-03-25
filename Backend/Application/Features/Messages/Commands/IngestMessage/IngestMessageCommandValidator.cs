using FluentValidation;

namespace WhatsAppParser.Application.Features.Messages.Commands.IngestMessage;

public sealed class IngestMessageCommandValidator : AbstractValidator<IngestMessageCommand>
{
    public IngestMessageCommandValidator()
    {
        RuleFor(x => x.RawText)
            .NotEmpty().WithMessage("RawText is required.")
            .MaximumLength(100_000).WithMessage("RawText must not exceed 100,000 characters.");
    }
}
