namespace Hpn.SharedKernel.Geo;

/// <summary>
/// Resolves the coarse ISO-3166-1 alpha-2 country of the current request from an
/// edge/CDN-provided geo header (e.g. Cloudflare's <c>CF-IPCountry</c>, set from the
/// client IP). The country is stored internally and used only for the inbound
/// same-country privacy filter — it is never shown on the feed, card, or profile
/// (ADR-028). Returns <c>null</c> when there is no trustworthy signal (local dev, a
/// missing header, or a placeholder such as <c>XX</c>/<c>T1</c>), in which case the
/// stored value is left untouched.
/// </summary>
public interface IClientCountryResolver
{
    string? ResolveCountryCode();
}
