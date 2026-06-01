using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Identity.Internal.Features.GetMe;

internal static class GetMeEndpoint
{
    public static IEndpointRouteBuilder MapGetMe(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/me", async (GetMeHandler handler, CancellationToken cancellationToken) =>
            {
                var me = await handler.HandleAsync(cancellationToken);
                return me is null ? Results.Unauthorized() : Results.Ok(me);
            })
            .RequireAuthorization()
            .WithName("GetMe")
            .WithSummary("The current account and its onboarding state.")
            .WithTags("Auth")
            .Produces<MeResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
