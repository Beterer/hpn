using Hpn.SharedKernel.Geo;

namespace Hpn.Api.Infrastructure;

/// <summary>
/// Estimates the request's country (ADR-028), internal-only, via a fallback chain:
/// <list type="number">
///   <item>the edge geo header (Cloudflare's <c>CF-IPCountry</c>) — cheapest and most
///   accurate when the deployment sits behind a CDN that sets it;</item>
///   <item>an offline GeoIP database lookup on the client IP (resolved from
///   X-Forwarded-For by the forwarded-headers middleware) — works without a CDN.</item>
/// </list>
/// Returns null when neither yields a valid ISO alpha-2 (local dev with a loopback IP
/// and no header, a placeholder like <c>XX</c>/<c>T1</c>, or no database present), which
/// leaves any stored country untouched.
/// </summary>
internal sealed class RequestClientCountryResolver(
    IHttpContextAccessor httpContextAccessor,
    GeoIpCountryDatabase geoIp) : IClientCountryResolver
{
    private static readonly string[] CountryHeaders = ["CF-IPCountry"];

    public string? ResolveCountryCode()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        foreach (var name in CountryHeaders)
        {
            if (Normalize(context.Request.Headers[name].ToString()) is { } fromHeader)
            {
                return fromHeader;
            }
        }

        return Normalize(geoIp.LookupCountry(context.Connection.RemoteIpAddress));
    }

    // A valid ISO alpha-2 only; reject Cloudflare placeholders XX (unknown) and T1 (Tor),
    // and anything that isn't two ASCII letters.
    private static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var code = raw.Trim().ToUpperInvariant();
        return code.Length == 2 && code.All(char.IsAsciiLetterUpper) && code is not ("XX" or "T1")
            ? code
            : null;
    }
}
