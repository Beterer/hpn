using Hpn.Modules.Identity.Internal.Accounts;
using Hpn.SharedKernel.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Identity.Internal.Features.ExportAccount;

/// <summary>
/// GDPR data export (§10.5): one JSON document gathering every module's slice of
/// the account, assembled through the cross-module orchestrator.
/// </summary>
internal static class ExportAccountEndpoint
{
    public static IEndpointRouteBuilder MapExportAccount(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/settings/account/export", async (
                ICurrentUser currentUser,
                AccountDataOrchestrator orchestrator,
                HttpResponse response,
                CancellationToken cancellationToken) =>
            {
                var bundle = await orchestrator.ExportAsync(currentUser.RequireUserId(), cancellationToken);
                response.Headers.ContentDisposition = "attachment; filename=\"notice-account-export.json\"";
                return Results.Ok(bundle);
            })
            .RequireAuthorization()
            .WithName("ExportAccount")
            .WithSummary("Download all of the current user's data (GDPR export).")
            .WithTags("Settings")
            .Produces<IReadOnlyDictionary<string, object?>>();

        return endpoints;
    }
}
