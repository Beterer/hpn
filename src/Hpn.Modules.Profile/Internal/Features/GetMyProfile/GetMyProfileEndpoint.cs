using Hpn.Modules.Profile.Internal.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Profile.Internal.Features.GetMyProfile;

internal static class GetMyProfileEndpoint
{
    public static IEndpointRouteBuilder MapGetMyProfile(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/profile/me", async (
                GetMyProfileHandler handler,
                CancellationToken cancellationToken) =>
            {
                var profile = await handler.HandleAsync(cancellationToken);
                return profile is null ? Results.NotFound() : Results.Ok(profile);
            })
            .RequireAuthorization()
            .WithName("GetMyProfile")
            .WithSummary("Get the current user's profile.")
            .WithTags("Profile")
            .Produces<ProfileResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
