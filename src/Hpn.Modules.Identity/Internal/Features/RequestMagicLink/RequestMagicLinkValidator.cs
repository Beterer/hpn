using FluentValidation;

namespace Hpn.Modules.Identity.Internal.Features.RequestMagicLink;

internal sealed class RequestMagicLinkValidator : AbstractValidator<RequestMagicLinkRequest>
{
    public RequestMagicLinkValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();
    }
}
