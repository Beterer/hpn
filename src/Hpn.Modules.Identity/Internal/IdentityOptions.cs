namespace Hpn.Modules.Identity.Internal;

/// <summary>
/// Identity tuning bound from the <c>Identity</c> configuration section. Defaults
/// are launch values (backbone §10.1); only the durations are expected to change.
/// </summary>
internal sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    /// <summary>Magic-link token lifetime (~15 min per §10.1).</summary>
    public TimeSpan MagicLinkLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Sliding session lifetime (§10.1).</summary>
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Max magic links issued per email inside one token lifetime window (per-email anti-abuse, §10.6).</summary>
    public int MaxMagicLinksPerEmailPerWindow { get; set; } = 3;

    /// <summary>Absolute URL the SPA serves its verify route from; the emailed link points here.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
