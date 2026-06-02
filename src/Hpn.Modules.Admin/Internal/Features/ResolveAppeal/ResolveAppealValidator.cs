using FluentValidation;

namespace Hpn.Modules.Admin.Internal.Features.ResolveAppeal;

internal sealed class ResolveAppealValidator : AbstractValidator<ResolveAppealRequest>
{
    private static readonly HashSet<string> AllowedOutcomes = new(StringComparer.Ordinal)
    {
        "upheld",
        "dismissed",
    };

    public ResolveAppealValidator()
    {
        RuleFor(x => x.TargetProfileId).NotEmpty();

        RuleFor(x => x.Outcome)
            .NotEmpty()
            .Must(outcome => AllowedOutcomes.Contains(outcome.Trim().ToLowerInvariant()))
            .WithMessage("Outcome must be upheld or dismissed.");

        RuleFor(x => x.Note)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(500);
    }
}
