using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Admin.Internal.Features.GetAdminReports;

internal static class GetAdminReportsEndpoint
{
    public static IEndpointRouteBuilder MapGetAdminReports(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/reports", async (
                [FromQuery] string? status,
                [FromQuery] int? limit,
                GetAdminReportsHandler handler,
                CancellationToken cancellationToken) =>
            {
                var reports = await handler.HandleAsync(status, limit, cancellationToken);
                return Results.Ok(reports);
            })
            .WithName("GetAdminReports")
            .WithSummary("List reports for internal review.")
            .Produces<IReadOnlyCollection<AdminReportResponse>>();

        return endpoints;
    }
}
