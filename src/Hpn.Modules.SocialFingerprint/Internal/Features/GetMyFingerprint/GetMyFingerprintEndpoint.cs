using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.SocialFingerprint.Internal.Features.GetMyFingerprint;

internal static class GetMyFingerprintEndpoint
{
    public static IEndpointRouteBuilder MapGetMyFingerprint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/fingerprint/me", async (
                GetMyFingerprintHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(cancellationToken);
                if (result.ProfileMissing)
                {
                    return Results.Problem(
                        title: "Profile required",
                        detail: "Create a profile before viewing your fingerprint.",
                        statusCode: StatusCodes.Status404NotFound,
                        type: "https://hpn.dev/problems/profile-required");
                }

                return Results.Ok(result.Response);
            })
            .RequireAuthorization()
            .WithName("GetMyFingerprint")
            .WithSummary("Show the current user's gated social fingerprint.")
            .WithTags("Social Fingerprint")
            .Produces<GetMyFingerprintResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
