using Hpn.Modules.Identity.Internal.Auth;
using Hpn.SharedKernel.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Identity.Internal.Features.StartGuestSession;

internal static class StartGuestSessionEndpoint
{
    public static IEndpointRouteBuilder MapStartGuestSession(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/guest/start", async (
                StartGuestSessionHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();
                var ip = httpContext.Connection.RemoteIpAddress?.ToString();
                httpContext.Request.Cookies.TryGetValue(GuestCookie.Name, out var existingGuestToken);

                var result = await handler.HandleAsync(
                    existingGuestToken,
                    string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
                    ip,
                    cancellationToken);

                if (result is { GuestToken: { } guestToken, ExpiresAt: { } expiresAt })
                {
                    GuestCookie.Append(httpContext.Response, guestToken, expiresAt);
                }

                return Results.Ok(result.Response);
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.GuestStart)
            .WithName("StartGuestSession")
            .WithSummary("Start or resume a guest browsing session.")
            .WithTags("Auth")
            .Produces<StartGuestSessionResponse>();

        return endpoints;
    }
}
