using Hpn.Modules.Profile.Internal.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Profile.Internal.Features.GetPublicProfile;

internal static class GetPublicProfileEndpoint
{
    public static IEndpointRouteBuilder MapGetPublicProfile(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/profiles/{id:guid}", async (
                Guid id,
                GetPublicProfileHandler handler,
                CancellationToken cancellationToken) =>
            {
                var profile = await handler.HandleAsync(id, cancellationToken);
                return profile is null ? Results.NotFound() : Results.Ok(profile);
            })
            .RequireAuthorization()
            .WithName("GetPublicProfile")
            .WithSummary("Get a visibility-checked public profile projection.")
            .WithTags("Profile")
            .Produces<PublicProfileResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
