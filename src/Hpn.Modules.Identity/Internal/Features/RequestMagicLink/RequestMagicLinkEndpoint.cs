using Hpn.SharedKernel.RateLimiting;
using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Identity.Internal.Features.RequestMagicLink;

internal static class RequestMagicLinkEndpoint
{
    public static IEndpointRouteBuilder MapRequestMagicLink(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/magic-link", async (
                RequestMagicLinkRequest request,
                RequestMagicLinkHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var requestIp = httpContext.Connection.RemoteIpAddress?.ToString();
                await handler.HandleAsync(request, requestIp, cancellationToken);

                // Always 202 — never reveal whether the account exists (backbone §10.1).
                return Results.Accepted();
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.MagicLink)
            .WithValidation<RequestMagicLinkRequest>()
            .WithName("RequestMagicLink")
            .WithSummary("Request a single-use magic sign-in link by email.")
            .Produces(StatusCodes.Status202Accepted);

        return endpoints;
    }
}
