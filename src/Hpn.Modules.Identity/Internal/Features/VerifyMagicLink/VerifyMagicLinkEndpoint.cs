using Hpn.Modules.Identity.Internal.Auth;
using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Identity.Internal.Features.VerifyMagicLink;

internal static class VerifyMagicLinkEndpoint
{
    public static IEndpointRouteBuilder MapVerifyMagicLink(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/verify", async (
                VerifyMagicLinkRequest request,
                VerifyMagicLinkHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();
                var ip = httpContext.Connection.RemoteIpAddress?.ToString();

                var result = await handler.HandleAsync(
                    request,
                    string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
                    ip,
                    cancellationToken);

                if (result is null)
                {
                    return Results.Problem(
                        title: "Invalid or expired sign-in link",
                        detail: "This sign-in link is no longer valid. Request a new one.",
                        statusCode: StatusCodes.Status400BadRequest,
                        type: "https://hpn.dev/problems/invalid-magic-link");
                }

                SessionCookie.Append(httpContext.Response, result.SessionToken, result.ExpiresAt);
                return Results.Ok(result.User);
            })
            .AllowAnonymous()
            .WithValidation<VerifyMagicLinkRequest>()
            .WithName("VerifyMagicLink")
            .WithSummary("Verify a magic-link token and start a session.")
            .Produces<AuthUserDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
