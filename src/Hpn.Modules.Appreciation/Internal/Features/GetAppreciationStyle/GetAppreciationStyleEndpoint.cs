using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Appreciation.Internal.Features.GetAppreciationStyle;

internal static class GetAppreciationStyleEndpoint
{
    public static IEndpointRouteBuilder MapGetAppreciationStyle(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/appreciation-style/me", async (
                GetAppreciationStyleHandler handler,
                CancellationToken cancellationToken) =>
            {
                var response = await handler.HandleAsync(cancellationToken);
                return Results.Ok(response);
            })
            .RequireAuthorization()
            .WithName("GetAppreciationStyle")
            .WithSummary("Show the current user's appreciation-style insights.")
            .WithTags("Appreciation")
            .Produces<GetAppreciationStyleResponse>();

        return endpoints;
    }
}
