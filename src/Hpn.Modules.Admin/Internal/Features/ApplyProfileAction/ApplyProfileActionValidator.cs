using FluentValidation;
using Hpn.Modules.Moderation.Contracts;

namespace Hpn.Modules.Admin.Internal.Features.ApplyProfileAction;

internal sealed class ApplyProfileActionValidator : AbstractValidator<ApplyProfileActionRequest>
{
    public ApplyProfileActionValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .Must(action => ModerationActions.All.Contains(action.Trim().ToLowerInvariant()))
            .WithMessage("Action must be warn, temp_restrict, ban, or clear.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(500);
    }
}
