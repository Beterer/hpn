using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Profile.Internal.Features.UpdateLocation;

internal static class UpdateLocationEndpoint
{
    public static IEndpointRouteBuilder MapUpdateLocation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/settings/location", async (
                UpdateLocationRequest request,
                UpdateLocationHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before setting your location.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                return Results.Ok(result.Location);
            })
            .RequireAuthorization()
            .WithValidation<UpdateLocationRequest>()
            .WithName("UpdateLocation")
            .WithSummary("Record or clear the current user's coarse location (with consent).")
            .WithTags("Settings")
            .Produces<LocationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return endpoints;
    }
}
