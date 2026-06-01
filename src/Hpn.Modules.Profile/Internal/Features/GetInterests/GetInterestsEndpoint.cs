using Hpn.Modules.Profile.Internal.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Profile.Internal.Features.GetInterests;

internal static class GetInterestsEndpoint
{
    public static IEndpointRouteBuilder MapGetInterests(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/interests", async (
                GetInterestsHandler handler,
                CancellationToken cancellationToken) =>
            {
                var interests = await handler.HandleAsync(cancellationToken);
                return Results.Ok(interests);
            })
            .RequireAuthorization()
            .WithName("GetInterests")
            .WithSummary("List seeded profile interests.")
            .WithTags("Profile")
            .Produces<IReadOnlyCollection<InterestResponse>>();

        return endpoints;
    }
}
