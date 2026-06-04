using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Identity.Internal.Auth;

/// <summary>
/// ASP.NET Core authentication handler for opaque member and guest cookies. Reads the
/// member cookie first, delegates validation/sliding to <see cref="SessionAuthenticator"/>
/// (resolved per-request so it gets a scoped DbContext), and reports anonymous
/// rather than challenging — this is an API, so the authorization middleware
/// turns missing auth into 401 (backbone §8.1, §10.1).
/// </summary>
internal sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Cookies.TryGetValue(SessionCookie.Name, out var rawToken) && !string.IsNullOrEmpty(rawToken))
        {
            var authenticator = Context.RequestServices.GetRequiredService<SessionAuthenticator>();
            var principal = await authenticator.AuthenticateAsync(rawToken, Context.RequestAborted);
            if (principal is not null)
            {
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
        }

        if (!Request.Cookies.TryGetValue(GuestCookie.Name, out var rawGuestToken) || string.IsNullOrEmpty(rawGuestToken))
        {
            return AuthenticateResult.NoResult();
        }

        var guestAuthenticator = Context.RequestServices.GetRequiredService<GuestSessionAuthenticator>();
        var guestPrincipal = await guestAuthenticator.AuthenticateAsync(rawGuestToken, Context.RequestAborted);
        if (guestPrincipal is null)
        {
            return AuthenticateResult.NoResult();
        }

        var guestTicket = new AuthenticationTicket(guestPrincipal, Scheme.Name);
        return AuthenticateResult.Success(guestTicket);
    }
}
