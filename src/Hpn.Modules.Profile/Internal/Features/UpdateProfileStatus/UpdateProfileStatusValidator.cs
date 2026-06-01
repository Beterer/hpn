using FluentValidation;

namespace Hpn.Modules.Profile.Internal.Features.UpdateProfileStatus;

internal sealed class UpdateProfileStatusValidator : AbstractValidator<UpdateProfileStatusRequest>
{
    public UpdateProfileStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(value => value is "active" or "paused")
            .WithMessage("Status must be active or paused.");
    }
}
