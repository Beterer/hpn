using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Admin.Internal.Features.GetAdminQueue;

internal static class GetAdminQueueEndpoint
{
    public static IEndpointRouteBuilder MapGetAdminQueue(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/queue", async (
                [FromQuery] int? limit,
                GetAdminQueueHandler handler,
                CancellationToken cancellationToken) =>
            {
                var queue = await handler.HandleAsync(limit, cancellationToken);
                return Results.Ok(queue);
            })
            .WithName("GetAdminQueue")
            .WithSummary("List profiles currently in the moderation review queue.")
            .Produces<IReadOnlyCollection<AdminQueueItemResponse>>();

        return endpoints;
    }
}
