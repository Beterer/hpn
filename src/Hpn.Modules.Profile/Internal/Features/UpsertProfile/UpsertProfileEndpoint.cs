using Hpn.Modules.Profile.Internal.Features;
using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Profile.Internal.Features.UpsertProfile;

internal static class UpsertProfileEndpoint
{
    public static IEndpointRouteBuilder MapUpsertProfile(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/profile", async (
                UpsertProfileRequest request,
                UpsertProfileHandler handler,
                CancellationToken cancellationToken) =>
            {
                var profile = await handler.HandleAsync(request, cancellationToken);
                return Results.Ok(profile);
            })
            .RequireAuthorization()
            .WithValidation<UpsertProfileRequest>()
            .WithName("UpsertProfile")
            .WithSummary("Create or edit the current user's profile.")
            .WithTags("Profile")
            .Produces<ProfileResponse>()
            .ProducesValidationProblem();

        return endpoints;
    }
}
