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
// IAppreciationApi.GetReceivedSummaryAsync (the cross-module contract the
// fingerprint consumes): this path adds presentation concerns — perception
// phrasing and recent events — and aggregates at the trait level (ADR-025),
// which the category-keyed projection does not carry. Trait counts are read on
// demand from the events rather than via a second projection table.
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

        var traitRows = await dbContext.AppreciationEvents
            .AsNoTracking()
            .Where(e => e.ReceiverProfileId == profileId)
            .Join(
                dbContext.AppreciationTraits.AsNoTracking(),
                e => e.TraitId,
                trait => trait.Id,
                (e, trait) => new { trait.Id, trait.Slug, trait.Label, trait.CategoryId, trait.SortOrder })
            .Join(
                dbContext.AppreciationCategories.AsNoTracking(),
                x => x.CategoryId,
                category => category.Id,
                (x, category) => new
                {
                    x.Id,
                    x.Slug,
                    x.Label,
                    CategorySlug = category.Slug,
                    CategoryLabel = category.Label,
                    category.Hue,
                    x.SortOrder,
                })
            .GroupBy(x => new { x.Id, x.Slug, x.Label, x.CategorySlug, x.CategoryLabel, x.Hue, x.SortOrder })
            .Select(g => new { g.Key, Count = g.Count() })
            .ToArrayAsync(cancellationToken);

        var traits = traitRows
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Key.SortOrder)
            .Select(r => new ReceivedAppreciationTraitResponse(
                r.Key.Id,
                r.Key.Slug,
                r.Key.Label,
                r.Key.CategorySlug,
                r.Key.CategoryLabel,
                r.Key.Hue,
                r.Count,
                ReceivedAppreciationPhrasing.ForTrait(r.Key.Slug, r.Key.Label)))
            .ToArray();

        var total = traits.Sum(t => t.Count);
        IReadOnlyCollection<ReceivedAppreciationEventResponse> events = includeEvents
            ? await GetRecentEventsAsync(profileId.Value, eventLimit, cancellationToken)
            : Array.Empty<ReceivedAppreciationEventResponse>();

        return GetReceivedAppreciationResult.Success(new GetReceivedAppreciationResponse(
            profileId.Value,
            ReceivedAppreciationPhrasing.Headline,
            total == 0 ? ReceivedAppreciationPhrasing.EmptySummary : ReceivedAppreciationPhrasing.Summary,
            total,
            traits,
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
                dbContext.AppreciationTraits.AsNoTracking(),
                appreciation => appreciation.TraitId,
                trait => trait.Id,
                (appreciation, trait) => new { appreciation, trait })
            .Join(
                dbContext.AppreciationCategories.AsNoTracking(),
                x => x.trait.CategoryId,
                category => category.Id,
                (x, category) => new
                {
                    x.appreciation.Id,
                    TraitId = x.trait.Id,
                    TraitSlug = x.trait.Slug,
                    TraitLabel = x.trait.Label,
                    CategorySlug = category.Slug,
                    category.Hue,
                    x.appreciation.PhotoId,
                    x.appreciation.CreatedAt,
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
                e.TraitId,
                e.TraitSlug,
                e.TraitLabel,
                e.CategorySlug,
                e.Hue,
                e.PhotoId,
                e.CreatedAt,
                ReceivedAppreciationPhrasing.ForEvent(e.TraitSlug, e.TraitLabel)))
            .ToArray();
    }
}
