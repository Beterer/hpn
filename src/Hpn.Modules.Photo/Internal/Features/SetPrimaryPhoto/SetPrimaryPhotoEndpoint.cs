using Hpn.Modules.Photo.Internal.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Photo.Internal.Features.SetPrimaryPhoto;

internal static class SetPrimaryPhotoEndpoint
{
    public static IEndpointRouteBuilder MapSetPrimaryPhoto(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/profile/photos/{id:guid}/primary", async (
                Guid id,
                SetPrimaryPhotoHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before managing photos.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                return result.PhotoMissing ? Results.NotFound() : Results.Ok(result.Photos);
            })
            .RequireAuthorization()
            .WithName("SetPrimaryPhoto")
            .WithSummary("Set the primary profile photo without changing photo order.")
            .WithTags("Photos")
            .Produces<IReadOnlyCollection<PhotoResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
