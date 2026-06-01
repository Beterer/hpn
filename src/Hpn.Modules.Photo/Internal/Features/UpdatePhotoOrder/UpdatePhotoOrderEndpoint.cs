using Hpn.Modules.Photo.Internal.Features;
using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Photo.Internal.Features.UpdatePhotoOrder;

internal static class UpdatePhotoOrderEndpoint
{
    public static IEndpointRouteBuilder MapUpdatePhotoOrder(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/profile/photos/order", async (
                UpdatePhotoOrderRequest request,
                UpdatePhotoOrderHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before managing photos.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                if (result.InvalidPhotoSet)
                {
                    return Results.Problem(
                        title: "Invalid photo order",
                        detail: "The order must contain every photo on your profile exactly once.",
                        statusCode: StatusCodes.Status400BadRequest,
                        type: "https://hpn.dev/problems/invalid-photo-order");
                }

                return Results.Ok(result.Photos);
            })
            .RequireAuthorization()
            .WithValidation<UpdatePhotoOrderRequest>()
            .WithName("UpdatePhotoOrder")
            .WithSummary("Reorder the current user's profile photos. Position 0 is primary.")
            .WithTags("Photos")
            .Produces<IReadOnlyCollection<PhotoResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return endpoints;
    }
}
