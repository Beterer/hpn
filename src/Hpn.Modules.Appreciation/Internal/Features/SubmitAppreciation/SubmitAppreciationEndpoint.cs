using Hpn.SharedKernel.RateLimiting;
using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Appreciation.Internal.Features.SubmitAppreciation;

internal static class SubmitAppreciationEndpoint
{
    public static IEndpointRouteBuilder MapSubmitAppreciation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/appreciations", async (
                SubmitAppreciationRequest request,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                SubmitAppreciationHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, idempotencyKey, cancellationToken);

                if (result.InvalidIdempotencyKey)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["Idempotency-Key"] = ["Send a non-empty Idempotency-Key header up to 128 characters."],
                    });
                }

                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before appreciating someone.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                if (result.SelfAppreciation)
                {
                    return Results.Problem(
                        title: "Choose another profile",
                        detail: "You cannot appreciate your own profile.",
                        statusCode: StatusCodes.Status409Conflict,
                        type: "https://hpn.dev/problems/self-appreciation");
                }

                if (result.ReceiverNotVisible)
                {
                    return Results.Problem(
                        title: "Profile unavailable",
                        detail: "That profile is not available to appreciate.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-unavailable");
                }

                if (result.CategoryMissing)
                {
                    return Results.Problem(
                        title: "Unknown appreciation category",
                        detail: "Choose one of the active appreciation categories.",
                        statusCode: StatusCodes.Status400BadRequest,
                        type: "https://hpn.dev/problems/appreciation-category-required");
                }

                if (result.PhotoMismatch)
                {
                    return Results.Problem(
                        title: "Photo unavailable",
                        detail: "That photo is not available for this profile.",
                        statusCode: StatusCodes.Status400BadRequest,
                        type: "https://hpn.dev/problems/photo-unavailable");
                }

                if (result.IdempotencyConflict)
                {
                    return Results.Problem(
                        title: "Idempotency key conflict",
                        detail: "This Idempotency-Key was already used for a different appreciation request.",
                        statusCode: StatusCodes.Status409Conflict,
                        type: "https://hpn.dev/problems/idempotency-key-conflict");
                }

                if (result.Duplicate)
                {
                    return Results.Problem(
                        title: "Already appreciated",
                        detail: "You have already chosen that appreciation for this profile.",
                        statusCode: StatusCodes.Status409Conflict,
                        type: "https://hpn.dev/problems/duplicate-appreciation");
                }

                return result.Replayed
                    ? Results.Ok(result.Response)
                    : Results.Created($"/api/v1/appreciations/{result.Response!.Id}", result.Response);
            })
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Appreciation)
            .WithValidation<SubmitAppreciationRequest>()
            .WithName("SubmitAppreciation")
            .WithSummary("Record a positive appreciation and unlock the next profile.")
            .WithTags("Appreciation")
            .Produces<SubmitAppreciationResponse>(StatusCodes.Status201Created)
            .Produces<SubmitAppreciationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return endpoints;
    }
}
