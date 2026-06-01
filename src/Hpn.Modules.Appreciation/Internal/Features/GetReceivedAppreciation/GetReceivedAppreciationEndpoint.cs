using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Appreciation.Internal.Features.GetReceivedAppreciation;

internal static class GetReceivedAppreciationEndpoint
{
    public static IEndpointRouteBuilder MapGetReceivedAppreciation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/appreciations/received", async (
                [FromQuery] bool? includeEvents,
                [FromQuery] int? eventLimit,
                GetReceivedAppreciationHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    includeEvents ?? false,
                    eventLimit ?? 0,
                    cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before viewing received appreciation.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                return Results.Ok(result.Response);
            })
            .RequireAuthorization()
            .WithName("GetReceivedAppreciation")
            .WithSummary("Show the current user's received appreciation with perception phrasing.")
            .WithTags("Appreciation")
            .Produces<GetReceivedAppreciationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
