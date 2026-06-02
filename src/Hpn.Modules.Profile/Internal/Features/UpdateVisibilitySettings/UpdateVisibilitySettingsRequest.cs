namespace Hpn.Modules.Profile.Internal.Features.UpdateVisibilitySettings;

internal sealed record UpdateVisibilitySettingsRequest(
    bool ShowOnlyOutsideCountry,
    bool HideFromCountry,
    int? MinDistanceKm,
    bool WomenForWomen,
    bool VerifiedOnly,
    bool Paused);
