using FluentValidation;
using Hpn.Modules.Moderation.Internal.Domain;

namespace Hpn.Modules.Moderation.Internal.Features.SubmitReport;

internal sealed class SubmitReportValidator : AbstractValidator<SubmitReportRequest>
{
    public SubmitReportValidator()
    {
        RuleFor(x => x.TargetProfileId).NotEmpty();

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => ModerationFormat.TryParseReportType(t, out _))
            .WithMessage("Type must be one of the known report categories.");

        RuleFor(x => x.Note)
            .MaximumLength(1000)
            .When(x => x.Note is not null);
    }
}
