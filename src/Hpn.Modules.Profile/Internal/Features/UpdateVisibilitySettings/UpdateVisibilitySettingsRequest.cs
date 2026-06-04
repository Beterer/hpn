namespace Hpn.Modules.Profile.Internal.Features.UpdateVisibilitySettings;

internal sealed record UpdateVisibilitySettingsRequest(
    bool HideFromCountry,
    int? MinDistanceKm,
    bool WomenForWomen,
    bool VerifiedOnly,
    bool Paused,
    bool HiddenFromGuests);
