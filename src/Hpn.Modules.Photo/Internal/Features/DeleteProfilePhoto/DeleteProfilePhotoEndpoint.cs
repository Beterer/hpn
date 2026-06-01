using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Photo.Internal.Features.DeleteProfilePhoto;

internal static class DeleteProfilePhotoEndpoint
{
    public static IEndpointRouteBuilder MapDeleteProfilePhoto(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/profile/photos/{id:guid}", async (
                Guid id,
                DeleteProfilePhotoHandler handler,
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

                return result.Removed ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization()
            .WithName("DeleteProfilePhoto")
            .WithSummary("Delete one of the current user's profile photos.")
            .WithTags("Photos")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
