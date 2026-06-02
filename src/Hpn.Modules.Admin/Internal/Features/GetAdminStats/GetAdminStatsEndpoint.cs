using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Admin.Internal.Features.GetAdminStats;

internal static class GetAdminStatsEndpoint
{
    public static IEndpointRouteBuilder MapGetAdminStats(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/stats", async (
                GetAdminStatsHandler handler,
                CancellationToken cancellationToken) =>
            {
                var stats = await handler.HandleAsync(cancellationToken);
                return Results.Ok(stats);
            })
            .WithName("GetAdminStats")
            .WithSummary("Show internal moderation stats.")
            .Produces<AdminStatsResponse>();

        return endpoints;
    }
}
