using Hpn.Modules.Profile.Internal.Features;
using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Profile.Internal.Features.UpdateProfileStatus;

internal static class UpdateProfileStatusEndpoint
{
    public static IEndpointRouteBuilder MapUpdateProfileStatus(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/profile/status", async (
                UpdateProfileStatusRequest request,
                UpdateProfileStatusHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before changing profile status.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                if (result.InvalidTransition)
                {
                    return Results.Problem(
                        title: "Invalid profile status transition",
                        detail: "Only a draft or paused profile can activate, and only an active profile can pause.",
                        statusCode: StatusCodes.Status409Conflict,
                        type: "https://hpn.dev/problems/invalid-profile-status-transition");
                }

                return Results.Ok(result.Profile);
            })
            .RequireAuthorization()
            .WithValidation<UpdateProfileStatusRequest>()
            .WithName("UpdateProfileStatus")
            .WithSummary("Activate or pause the current user's profile.")
            .WithTags("Profile")
            .Produces<ProfileResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return endpoints;
    }
}
