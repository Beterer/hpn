using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Identity.Internal.Features.RequestAccountDeletion;

internal sealed record AccountDeletionResponse(DateTimeOffset? PurgeAfter);

internal static class RequestAccountDeletionEndpoint
{
    public static IEndpointRouteBuilder MapRequestAccountDeletion(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/settings/account/delete", async (
                RequestAccountDeletionHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cancellationToken);
                if (result.UserMissing)
                {
                    return Results.Problem(
                        title: "Account not found",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/account-not-found");
                }

                // 202: the account is soft-deleted now; the hard purge happens after
                // the grace window (§10.5).
                return Results.Accepted(value: new AccountDeletionResponse(result.PurgeAfter));
            })
            .RequireAuthorization()
            .WithName("RequestAccountDeletion")
            .WithSummary("Start account deletion (soft-delete now, hard purge after the grace window).")
            .WithTags("Settings")
            .Produces<AccountDeletionResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
