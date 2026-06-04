namespace Hpn.Modules.Profile.Internal.Domain;

internal sealed class VisibilityPreferences
{
    public Guid ProfileId { get; private set; }
    public bool HideFromCountry { get; private set; }
    public int? MinDistanceKm { get; private set; }
    public bool WomenForWomen { get; private set; }
    public bool VerifiedOnly { get; private set; }
    public bool Paused { get; private set; }
    public bool HiddenFromGuests { get; private set; }

    private VisibilityPreferences()
    {
    }

    public static VisibilityPreferences Create(Guid profileId) => new()
    {
        ProfileId = profileId,
        HideFromCountry = false,
        MinDistanceKm = null,
        WomenForWomen = false,
        VerifiedOnly = false,
        Paused = false,
        HiddenFromGuests = false,
    };

    public void Pause() => Paused = true;

    public void Resume() => Paused = false;

    /// <summary>Applies the full set of audience/privacy toggles from settings (§7.3, §8).</summary>
    public void Update(
        bool hideFromCountry,
        int? minDistanceKm,
        bool womenForWomen,
        bool verifiedOnly,
        bool paused,
        bool hiddenFromGuests)
    {
        HideFromCountry = hideFromCountry;
        MinDistanceKm = minDistanceKm;
        WomenForWomen = womenForWomen;
        VerifiedOnly = verifiedOnly;
        Paused = paused;
        HiddenFromGuests = hiddenFromGuests;
    }
}
