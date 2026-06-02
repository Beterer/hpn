using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Profile.Internal.Features.ManageBlocks;

internal static class ManageBlocksEndpoints
{
    public static IEndpointRouteBuilder MapManageBlocks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/settings/blocks", async (
                BlockUserRequest request,
                ManageBlocksHandler handler,
                CancellationToken cancellationToken) =>
            {
                var outcome = await handler.BlockAsync(request.TargetProfileId, cancellationToken);
                return outcome switch
                {
                    BlockOutcome.TargetMissing => Results.Problem(
                        title: "Profile not found",
                        detail: "The profile you tried to block does not exist.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-not-found"),
                    BlockOutcome.CannotBlockSelf => Results.Problem(
                        title: "Cannot block yourself",
                        detail: "You cannot block your own profile.",
                        statusCode: StatusCodes.Status400BadRequest,
                        type: "https://hpn.dev/problems/cannot-block-self"),
                    _ => Results.NoContent(),
                };
            })
            .RequireAuthorization()
            .WithValidation<BlockUserRequest>()
            .WithName("BlockUser")
            .WithSummary("Block a profile (honoured in both directions across the feed).")
            .WithTags("Settings")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        endpoints.MapDelete("/settings/blocks/{profileId:guid}", async (
                Guid profileId,
                ManageBlocksHandler handler,
                CancellationToken cancellationToken) =>
            {
                var outcome = await handler.UnblockAsync(profileId, cancellationToken);
                return outcome == BlockOutcome.TargetMissing
                    ? Results.Problem(
                        title: "Profile not found",
                        detail: "The profile you tried to unblock does not exist.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-not-found")
                    : Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("UnblockUser")
            .WithSummary("Remove a block.")
            .WithTags("Settings")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet("/settings/blocks", async (
                ManageBlocksHandler handler,
                CancellationToken cancellationToken) =>
            {
                var blocked = await handler.ListAsync(cancellationToken);
                return Results.Ok(blocked);
            })
            .RequireAuthorization()
            .WithName("ListBlocks")
            .WithSummary("List the profiles the current user has blocked.")
            .WithTags("Settings")
            .Produces<IReadOnlyCollection<BlockedProfileResponse>>();

        return endpoints;
    }
}
