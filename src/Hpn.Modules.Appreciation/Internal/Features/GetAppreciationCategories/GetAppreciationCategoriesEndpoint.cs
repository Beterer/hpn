using Hpn.Modules.Appreciation.Contracts.Dtos;
using Hpn.SharedKernel.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Appreciation.Internal.Features.GetAppreciationCategories;

internal static class GetAppreciationCategoriesEndpoint
{
    public static IEndpointRouteBuilder MapGetAppreciationCategories(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/appreciation-categories", async (
                GetAppreciationCategoriesHandler handler,
                CancellationToken cancellationToken) =>
            {
                var categories = await handler.HandleAsync(cancellationToken);
                return Results.Ok(categories);
            })
            .RequireAuthorization(Policies.GuestOrMember)
            .WithName("GetAppreciationCategories")
            .WithSummary("List the fixed seeded appreciation categories.")
            .WithTags("Appreciation")
            .Produces<IReadOnlyCollection<AppreciationCategoryDto>>();

        return endpoints;
    }
}
