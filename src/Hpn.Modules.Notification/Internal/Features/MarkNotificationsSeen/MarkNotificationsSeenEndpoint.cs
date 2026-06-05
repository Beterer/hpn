using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Notification.Internal.Features.MarkNotificationsSeen;

internal static class MarkNotificationsSeenEndpoint
{
    public static IEndpointRouteBuilder MapMarkNotificationsSeen(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/notifications/seen", async (
                MarkNotificationsSeenHandler handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("MarkNotificationsSeen")
            .WithSummary("Mark all of the caller's unseen notifications seen.")
            .WithTags("Notification")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }
}
