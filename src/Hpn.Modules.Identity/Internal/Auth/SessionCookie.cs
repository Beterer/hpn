using Microsoft.AspNetCore.Http;

namespace Hpn.Modules.Identity.Internal.Auth;

/// <summary>
/// Centralizes the session cookie name and attributes so issuing and clearing
/// stay in lock-step. httpOnly + Secure + SameSite=Lax, holding only the opaque
/// session secret — never a token the SPA can read (backbone §10.1, §11).
/// </summary>
internal static class SessionCookie
{
    public const string Name = "hpn_session";

    public static void Append(HttpResponse response, string rawToken, DateTimeOffset expiresAt) =>
        response.Cookies.Append(Name, rawToken, BuildOptions(expiresAt));

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, BuildOptions(expiresAt: null));

    private static CookieOptions BuildOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expiresAt,
        IsEssential = true,
    };
}
