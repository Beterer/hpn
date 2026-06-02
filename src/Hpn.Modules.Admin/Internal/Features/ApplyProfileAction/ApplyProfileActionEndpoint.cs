using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Admin.Internal.Features.ApplyProfileAction;

internal static class ApplyProfileActionEndpoint
{
    public static IEndpointRouteBuilder MapApplyProfileAction(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/profiles/{id:guid}/action", async (
                Guid id,
                ApplyProfileActionRequest request,
                ApplyProfileActionHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(id, request, cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile not found",
                        detail: "No profile exists for that id.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/admin-profile-not-found");
                }

                return Results.Ok(result.Response);
            })
            .WithValidation<ApplyProfileActionRequest>()
            .WithName("ApplyAdminProfileAction")
            .WithSummary("Apply an admin moderation decision to a profile.")
            .Produces<ApplyProfileActionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return endpoints;
    }
}
