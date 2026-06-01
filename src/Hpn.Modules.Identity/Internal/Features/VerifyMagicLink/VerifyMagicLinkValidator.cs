using FluentValidation;

namespace Hpn.Modules.Identity.Internal.Features.VerifyMagicLink;

internal sealed class VerifyMagicLinkValidator : AbstractValidator<VerifyMagicLinkRequest>
{
    public VerifyMagicLinkValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(512);
    }
}
