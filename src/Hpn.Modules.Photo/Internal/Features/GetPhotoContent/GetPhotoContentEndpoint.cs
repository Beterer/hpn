using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Photo.Internal.Features.GetPhotoContent;

internal static class GetPhotoContentEndpoint
{
    public static IEndpointRouteBuilder MapGetPhotoContent(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/profile/photos/{id:guid}/content", async (
                Guid id,
                string? variant,
                GetPhotoContentHandler handler,
                HttpResponse response,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, variant, cancellationToken);
                if (result.InvalidVariant)
                {
                    return Results.Problem(
                        title: "Invalid photo variant",
                        detail: "Only display and thumb variants are available.",
                        statusCode: StatusCodes.Status400BadRequest,
                        type: "https://hpn.dev/problems/invalid-photo-variant");
                }

                if (result.NotFound || result.Content is null || result.ContentType is null)
                {
                    return Results.NotFound();
                }

                // Content is immutable (keyed by photo id + variant) and owner-scoped,
                // so it is safe to cache privately rather than re-proxy every request.
                response.Headers.CacheControl = "private, max-age=3600, immutable";
                return Results.File(result.Content, result.ContentType);
            })
            .RequireAuthorization()
            .WithName("GetPhotoContent")
            .WithSummary("Read a processed profile photo variant owned by the current user.")
            .WithTags("Photos")
            .Produces(StatusCodes.Status200OK, contentType: "image/webp")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
