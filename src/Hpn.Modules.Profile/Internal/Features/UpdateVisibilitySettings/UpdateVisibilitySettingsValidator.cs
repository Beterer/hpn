using FluentValidation;

namespace Hpn.Modules.Profile.Internal.Features.UpdateVisibilitySettings;

internal sealed class UpdateVisibilitySettingsValidator : AbstractValidator<UpdateVisibilitySettingsRequest>
{
    // Half the earth's circumference — past this "minimum distance" excludes everyone.
    private const int MaxDistanceKm = 20_000;

    public UpdateVisibilitySettingsValidator()
    {
        RuleFor(x => x.MinDistanceKm)
            .InclusiveBetween(0, MaxDistanceKm)
            .When(x => x.MinDistanceKm is not null)
            .WithMessage($"Minimum distance must be between 0 and {MaxDistanceKm} km.");
    }
}
