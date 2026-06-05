namespace Hpn.SharedKernel.RateLimiting;

/// <summary>
/// Named rate-limit policies (backbone §10.6). The host registers the limiter
/// for each name; module endpoints opt in via <c>RequireRateLimiting</c>. Shared
/// here so the two sides can never drift on the string. Limits are launch
/// defaults to tune with real traffic.
/// </summary>
public static class RateLimitPolicies
{
    public const string MagicLink = "magic-link";
    public const string GuestStart = "guest-start";
    public const string Appreciation = "appreciation";
    public const string Reports = "reports";
    public const string Uploads = "uploads";
    public const string Notifications = "notifications";
}
