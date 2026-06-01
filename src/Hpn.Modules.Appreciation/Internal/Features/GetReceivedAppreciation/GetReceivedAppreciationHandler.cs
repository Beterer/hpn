using Hpn.Modules.Appreciation.Internal.Persistence;
using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Appreciation.Internal.Features.GetReceivedAppreciation;

internal sealed record GetReceivedAppreciationResult(
    GetReceivedAppreciationResponse? Response,
    bool ProfileMissing)
{
    public static GetReceivedAppreciationResult Success(GetReceivedAppreciationResponse response) =>
        new(response, ProfileMissing: false);

    public static GetReceivedAppreciationResult MissingProfile() =>
        new(null, ProfileMissing: true);
}

// Owner-facing read of received appreciation. Intentionally separate from
// IAppreciationApi.GetReceivedSummaryAsync (the cross-module contract M7's
// fingerprint consumes): this path adds presentation concerns — perception
// phrasing and recent events — that don't belong in a contract DTO. Keep them
// distinct rather than collapsing them.
internal sealed class GetReceivedAppreciationHandler(
    AppreciationDbContext dbContext,
    ICurrentUser currentUser,
    IProfileApi profileApi)
{
    private const int DefaultEventLimit = 10;
    private const int MaxEventLimit = 50;

    public async Task<GetReceivedAppreciationResult> HandleAsync(
        bool includeEvents,
        int eventLimit,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);
        if (profileId is null)
        {
            return GetReceivedAppreciationResult.MissingProfile();
        }

        var categoryRows = await dbContext.ReceivedAppreciationStats
            .AsNoTracking()
            .Where(s => s.ReceiverProfileId == profileId)
            .Join(
                dbContext.AppreciationCategories.AsNoTracking(),
                stat => stat.CategoryId,
                category => category.Id,
                (stat, category) => new
                {
                    category.Id,
                    category.Slug,
                    category.Label,
                    category.SortOrder,
                    stat.Count,
                })
            .OrderBy(c => c.SortOrder)
            .ToArrayAsync(cancellationToken);

        var categories = categoryRows
            .Select(c => new ReceivedAppreciationCategoryResponse(
                c.Id,
                c.Slug,
                c.Label,
                c.Count,
                ReceivedAppreciationPhrasing.ForCategory(c.Slug, c.Label)))
            .ToArray();

        var total = categories.Sum(c => c.Count);
        IReadOnlyCollection<ReceivedAppreciationEventResponse> events = includeEvents
            ? await GetRecentEventsAsync(profileId.Value, eventLimit, cancellationToken)
            : Array.Empty<ReceivedAppreciationEventResponse>();

        return GetReceivedAppreciationResult.Success(new GetReceivedAppreciationResponse(
            profileId.Value,
            ReceivedAppreciationPhrasing.Headline,
            total == 0 ? ReceivedAppreciationPhrasing.EmptySummary : ReceivedAppreciationPhrasing.Summary,
            total,
            categories,
            events));
    }

    private async Task<IReadOnlyCollection<ReceivedAppreciationEventResponse>> GetRecentEventsAsync(
        Guid profileId,
        int eventLimit,
        CancellationToken cancellationToken)
    {
        var requestedLimit = eventLimit <= 0 ? DefaultEventLimit : eventLimit;
        var limit = Math.Clamp(requestedLimit, 1, MaxEventLimit);
        var events = await dbContext.AppreciationEvents
            .AsNoTracking()
            .Where(e => e.ReceiverProfileId == profileId)
            .Join(
                dbContext.AppreciationCategories.AsNoTracking(),
                appreciation => appreciation.CategoryId,
                category => category.Id,
                (appreciation, category) => new
                {
                    appreciation.Id,
                    CategoryId = category.Id,
                    CategorySlug = category.Slug,
                    CategoryLabel = category.Label,
                    appreciation.PhotoId,
                    appreciation.CreatedAt,
                })
            // Id is UUIDv7 (time-ordered), so it breaks created_at ties stably —
            // recent-events paging stays deterministic across calls.
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        return events
            .Select(e => new ReceivedAppreciationEventResponse(
                e.Id,
                e.CategoryId,
                e.CategorySlug,
                e.CategoryLabel,
                e.PhotoId,
                e.CreatedAt,
                ReceivedAppreciationPhrasing.ForEvent(e.CategorySlug, e.CategoryLabel)))
            .ToArray();
    }
}
