using Hpn.Modules.Feed.Contracts.Dtos;
using Hpn.SharedKernel.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hpn.Modules.Feed.Internal.Features.GetNext;

internal static class GetFeedNextEndpoint
{
    public static IEndpointRouteBuilder MapGetFeedNext(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/feed/next", async (
                int? limit,
                string? seen,
                ICurrentUser currentUser,
                GetFeedNextHandler handler,
                CancellationToken cancellationToken) =>
            {
                var seenIds = ParseSeen(seen);
                var batch = currentUser.UserId is { } viewerUserId
                    ? await handler.HandleAsync(viewerUserId, limit, seenIds, cancellationToken)
                    : await handler.HandleForGuestAsync(currentUser.RequireActorId(), limit, seenIds, cancellationToken);
                return Results.Ok(batch);
            })
            .RequireAuthorization(Policies.GuestOrMember)
            .WithName("GetFeedNext")
            .WithSummary("The next batch of eligible profiles for the current viewer.")
            .WithDescription(
                "Ranking is delegated to the active feed strategy (v1: random within eligible). " +
                "Pass already-seen profile ids via the comma-separated 'seen' parameter for " +
                "session-level dedupe; the client drives the prefetch queue.")
            .WithTags("Feed")
            .Produces<IReadOnlyList<FeedProfileDto>>();

        return endpoints;
    }

    private static IReadOnlyCollection<Guid> ParseSeen(string? seen)
    {
        if (string.IsNullOrWhiteSpace(seen))
        {
            return [];
        }

        var ids = new List<Guid>();
        foreach (var part in seen.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}
