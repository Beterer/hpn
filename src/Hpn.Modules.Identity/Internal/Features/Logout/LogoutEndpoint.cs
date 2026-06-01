using Hpn.Modules.Identity.Internal.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Identity.Internal.Features.Logout;

internal static class LogoutEndpoint
{
    public static IEndpointRouteBuilder MapLogout(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/logout", async (
                LogoutHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                httpContext.Request.Cookies.TryGetValue(SessionCookie.Name, out var rawToken);
                await handler.HandleAsync(rawToken, cancellationToken);

                SessionCookie.Delete(httpContext.Response);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("Logout")
            .WithSummary("Revoke the current session and clear the cookie.")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }
}
