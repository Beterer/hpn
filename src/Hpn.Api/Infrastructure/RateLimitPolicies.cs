namespace Hpn.Api.Infrastructure;

/// <summary>
/// Named rate-limit policies (backbone §10.6). Endpoints opt into a policy per
/// milestone; the limits here are launch defaults to tune with real traffic.
/// </summary>
internal static class RateLimitPolicies
{
    public const string MagicLink = "magic-link";
    public const string Appreciation = "appreciation";
    public const string Reports = "reports";
    public const string Uploads = "uploads";
}
