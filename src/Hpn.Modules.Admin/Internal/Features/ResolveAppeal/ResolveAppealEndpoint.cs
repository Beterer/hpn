using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Admin.Internal.Features.ResolveAppeal;

internal static class ResolveAppealEndpoint
{
    public static IEndpointRouteBuilder MapResolveAppeal(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/appeals/{id:guid}/resolve", async (
                Guid id,
                ResolveAppealRequest request,
                ResolveAppealHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, request, cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile not found",
                        detail: "No profile exists for that id.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/admin-profile-not-found");
                }

                return Results.Ok(result.Response);
            })
            .WithValidation<ResolveAppealRequest>()
            .WithName("ResolveAdminAppeal")
            .WithSummary("Resolve an admin appeal; an upheld outcome lifts the restriction.")
            .Produces<ResolveAppealResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return endpoints;
    }
}
