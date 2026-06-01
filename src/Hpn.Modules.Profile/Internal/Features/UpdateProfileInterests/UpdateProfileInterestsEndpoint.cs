using Hpn.Modules.Profile.Internal.Features;
using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Profile.Internal.Features.UpdateProfileInterests;

internal static class UpdateProfileInterestsEndpoint
{
    public static IEndpointRouteBuilder MapUpdateProfileInterests(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/profile/interests", async (
                UpdateProfileInterestsRequest request,
                UpdateProfileInterestsHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before choosing interests.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                if (result.UnknownInterest)
                {
                    return Results.Problem(
                        title: "Unknown interest",
                        detail: "One or more selected interests do not exist.",
                        statusCode: StatusCodes.Status400BadRequest,
                        type: "https://hpn.dev/problems/unknown-interest");
                }

                return Results.Ok(result.Profile);
            })
            .RequireAuthorization()
            .WithValidation<UpdateProfileInterestsRequest>()
            .WithName("UpdateProfileInterests")
            .WithSummary("Replace the current user's profile interests.")
            .WithTags("Profile")
            .Produces<ProfileResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return endpoints;
    }
}
