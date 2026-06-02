using FluentValidation;

namespace Hpn.Modules.Profile.Internal.Features.UpdateLocation;

internal sealed class UpdateLocationValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationValidator()
    {
        // Coordinates are only meaningful (and only required) when consent is given.
        When(x => x.Consent, () =>
        {
            RuleFor(x => x.Latitude)
                .NotNull().InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90.");
            RuleFor(x => x.Longitude)
                .NotNull().InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180.");
        });
    }
}
