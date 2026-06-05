using Hpn.SharedKernel.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Notification.Internal.Features.GetNotificationSummary;

internal static class GetNotificationSummaryEndpoint
{
    public static IEndpointRouteBuilder MapGetNotificationSummary(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/notifications/summary", async (
                GetNotificationSummaryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Notifications)
            .WithName("GetNotificationSummary")
            .WithSummary("Unseen-notification count and latest notification.")
            .WithTags("Notification")
            .Produces<GetNotificationSummaryResponse>();

        return endpoints;
    }
}
