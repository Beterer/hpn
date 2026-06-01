using Hpn.Modules.Photo.Internal.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Photo.Internal.Features.GetMyPhotos;

internal static class GetMyPhotosEndpoint
{
    public static IEndpointRouteBuilder MapGetMyPhotos(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/profile/photos", async (
                GetMyPhotosHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before managing photos.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                return Results.Ok(result.Photos);
            })
            .RequireAuthorization()
            .WithName("GetMyPhotos")
            .WithSummary("List the current user's profile photos.")
            .WithTags("Photos")
            .Produces<IReadOnlyCollection<PhotoResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
