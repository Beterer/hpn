using Microsoft.AspNetCore.Http;

namespace Hpn.Modules.Identity.Internal.Auth;

internal static class GuestCookie
{
    public const string Name = "hpn_guest";

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
