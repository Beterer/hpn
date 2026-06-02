using Hpn.SharedKernel.RateLimiting;
using Hpn.SharedKernel.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Moderation.Internal.Features.SubmitReport;

internal static class SubmitReportEndpoint
{
    public static IEndpointRouteBuilder MapSubmitReport(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/reports", async (
                SubmitReportRequest request,
                SubmitReportHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);

                if (result.TargetMissing)
                {
                    return Results.Problem(
                        title: "Profile not found",
                        detail: "That profile does not exist.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/report-target-missing");
                }

                if (result.SelfReport)
                {
                    return Results.Problem(
                        title: "Cannot report yourself",
                        detail: "You cannot report your own profile.",
                        statusCode: StatusCodes.Status400BadRequest,
                        type: "https://hpn.dev/problems/report-self");
                }

                // Always 202: intake is acknowledged, never an outcome (§2). Duplicate
                // submits collapse to the same acknowledgement.
                return Results.Accepted($"/api/v1/reports/{result.Response!.ReportId}", result.Response);
            })
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Reports)
            .WithValidation<SubmitReportRequest>()
            .WithName("SubmitReport")
            .WithSummary("Report a profile for review.")
            .WithTags("Reports")
            .Produces<SubmitReportResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return endpoints;
    }
}
