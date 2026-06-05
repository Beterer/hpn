using Hpn.Modules.Photo.Internal.Features;
using Hpn.Modules.Photo.Internal.ImageProcessing;
using Hpn.SharedKernel.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Hpn.Modules.Photo.Internal.Features.UploadProfilePhoto;

internal static class UploadProfilePhotoEndpoint
{
    public static IEndpointRouteBuilder MapUploadProfilePhoto(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/profile/photos", async (
                [FromForm] IFormFile file,
                UploadProfilePhotoHandler handler,
                IOptions<PhotoUploadOptions> options,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(file, cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before uploading photos.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                if (result.LimitReached)
                {
                    return Results.Problem(
                        title: "Photo limit reached",
                        detail: $"Profiles can have up to {options.Value.MaxPhotosPerProfile} photos.",
                        statusCode: StatusCodes.Status409Conflict,
                        type: "https://hpn.dev/problems/photo-limit-reached");
                }

                if (result.ValidationProblem is not null)
                {
                    return Results.Problem(
                        title: "Photo rejected",
                        detail: result.ValidationProblem,
                        statusCode: StatusCodes.Status400BadRequest,
                        type: "https://hpn.dev/problems/photo-rejected");
                }

                return Results.Ok(result.Photo);
            })
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Uploads)
            .DisableAntiforgery()
            .WithName("UploadProfilePhoto")
            .WithSummary("Upload, validate, process, and store a profile photo.")
            .WithTags("Photos")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<PhotoResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
}
