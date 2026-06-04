using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Hpn.SharedKernel.Auth;

namespace Hpn.Modules.Photo.Internal.Features.GetPublicPhotoContent;

internal static class GetPublicPhotoContentEndpoint
{
    public static IEndpointRouteBuilder MapGetPublicPhotoContent(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/photos/{id:guid}/content", async (
                Guid id,
                string? variant,
                GetPublicPhotoContentHandler handler,
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

                // Immutable bytes (keyed by photo id + variant), but visibility is
                // per-viewer — so cache privately, never shared.
                response.Headers.CacheControl = "private, max-age=3600, immutable";
                return Results.File(result.Content, result.ContentType);
            })
            .RequireAuthorization(Policies.GuestOrMember)
            .WithName("GetPublicPhotoContent")
            .WithSummary("Read a visibility-checked profile photo variant for the feed.")
            .WithTags("Photos")
            .Produces(StatusCodes.Status200OK, contentType: "image/webp")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
