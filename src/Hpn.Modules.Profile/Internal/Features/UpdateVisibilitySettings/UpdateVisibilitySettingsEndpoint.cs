using Hpn.Modules.Profile.Internal.Features;
using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Profile.Internal.Features.UpdateVisibilitySettings;

internal static class UpdateVisibilitySettingsEndpoint
{
    public static IEndpointRouteBuilder MapUpdateVisibilitySettings(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/settings/visibility", async (
                UpdateVisibilitySettingsRequest request,
                UpdateVisibilitySettingsHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before changing visibility settings.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                if (result.FailedRequirement is { } failedRequirement)
                {
                    // Un-pausing re-activates the profile, so it must meet the same
                    // requirements as activation (e.g. at least one ready photo).
                    return Results.Problem(
                        title: failedRequirement.Title,
                        detail: failedRequirement.Detail,
                        statusCode: StatusCodes.Status409Conflict,
                        type: failedRequirement.ProblemType);
                }

                return Results.Ok(result.Preferences);
            })
            .RequireAuthorization()
            .WithValidation<UpdateVisibilitySettingsRequest>()
            .WithName("UpdateVisibilitySettings")
            .WithSummary("Update the current user's audience and privacy toggles.")
            .WithTags("Settings")
            .Produces<VisibilityPreferencesResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return endpoints;
    }
}
