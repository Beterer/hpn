using Hpn.Modules.Feed.Contracts.Dtos;
using Hpn.Modules.Feed.Internal.Persistence;
using Hpn.Modules.Feed.Internal.Ranking;
using Hpn.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Feed.Internal.Features.GetNext;

/// <summary>
/// The feed pipeline (backbone §6.5): <c>eligibility query → candidate set →
/// IFeedRankingStrategy.Select → batch</c>. Eligibility is the stable half — the
/// hard filters that decide whether a profile <em>may</em> be shown. Ordering and
/// selection are delegated wholesale to the injected strategy, so changing the
/// algorithm never touches this query.
/// </summary>
internal sealed class GetFeedNextHandler(FeedDbContext dbContext, IFeedRankingStrategy strategy)
{
    public const int DefaultLimit = 10;
    public const int MaxLimit = 20;

    // Cap the candidate pool the strategy ranks over, so the query and the
    // in-memory selection stay cheap on the latency-sensitive read path (§3.4).
    // The pool is a *random sample* of the eligible set, not an id-ordered prefix:
    // profile ids are time-ordered (UUIDv7), so taking the first N would bias every
    // strategy toward the oldest profiles once eligibility exceeds the cap. Sampling
    // is a neutral bound, not a ranking — display order remains wholly the
    // strategy's call (§6.5). A future strategy needing the *whole* eligible set
    // with its own signals would widen this input, as the backbone anticipates.
    private const int CandidatePoolSize = 200;

    // Bound the client-supplied session-dedupe list so a hostile/huge query string
    // can't blow up the SQL parameter list (§7.6 — session-level dedupe, no table).
    private const int MaxSeen = 200;

    public async Task<IReadOnlyList<FeedProfileDto>> HandleAsync(
        Guid viewerUserId,
        int? limit,
        IReadOnlyCollection<Guid> seenProfileIds,
        CancellationToken cancellationToken)
    {
        var batchSize = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

        // The viewer's own profile drives the audience/country rules below. No
        // profile yet (mid-onboarding) → nothing to browse.
        var viewer = await dbContext.Profiles
            .AsNoTracking()
            .Where(p => p.UserId == viewerUserId)
            .Select(p => new FeedViewerContext(p.Id, p.UserId, p.Gender, p.CountryCode, p.Verified))
            .FirstOrDefaultAsync(cancellationToken);
        if (viewer is null)
        {
            return [];
        }

        var viewerPrefs = await dbContext.VisibilityPreferences
            .AsNoTracking()
            .Where(v => v.ProfileId == viewer.ProfileId)
            .FirstOrDefaultAsync(cancellationToken);

        var viewerIsWoman = viewer.Gender == "woman";
        var viewerWantsWomenOnly = viewerPrefs?.WomenForWomen ?? false;
        var viewerWantsVerifiedOnly = viewerPrefs?.VerifiedOnly ?? false;
        var viewerWantsOutsideCountry = viewerPrefs?.ShowOnlyOutsideCountry ?? false;
        var viewerCountry = viewer.CountryCode;

        var seen = seenProfileIds.Take(MaxSeen).ToArray();

        var eligible = BuildEligibilityQuery(
            viewerUserId,
            viewer,
            viewerIsWoman,
            viewerWantsWomenOnly,
            viewerWantsVerifiedOnly,
            viewerWantsOutsideCountry,
            viewerCountry,
            seen);

        var eligibleIds = await eligible
            .OrderBy(_ => EF.Functions.Random())
            .Take(CandidatePoolSize)
            .ToListAsync(cancellationToken);

        var selectedIds = strategy.Select(eligibleIds, viewer, batchSize);
        if (selectedIds.Count == 0)
        {
            return [];
        }

        var cards = await dbContext.Profiles
            .AsNoTracking()
            .Where(p => selectedIds.Contains(p.Id))
            .Select(p => new FeedProfileDto(
                p.Id,
                p.DisplayName,
                p.Gender,
                p.SelfDescribeText,
                p.CountryCode,
                p.Bio,
                p.Verified,
                dbContext.Photos
                    .Where(ph => ph.ProfileId == p.Id && ph.Status == "ready")
                    .OrderBy(ph => ph.Position)
                    .Select(ph => new FeedPhotoDto(
                        ph.Id,
                        ph.Position,
                        ph.Width,
                        ph.Height,
                        $"{ApiRoutes.Prefix}/photos/{ph.Id}/content?variant=display",
                        $"{ApiRoutes.Prefix}/photos/{ph.Id}/content?variant=thumb"))
                    .ToList()))
            .ToListAsync(cancellationToken);

        // Restore the strategy's chosen order (the DB returned cards unordered).
        var byId = cards.ToDictionary(c => c.ProfileId);
        return [.. selectedIds.Where(byId.ContainsKey).Select(id => byId[id])];
    }

    /// <summary>
    /// The eligibility query — the stable hard filters (backbone §6.5). Returns the
    /// profile ids a viewer is permitted to see. Volatile ordering/selection is the
    /// strategy's job and is intentionally absent here.
    /// </summary>
    private IQueryable<Guid> BuildEligibilityQuery(
        Guid viewerUserId,
        FeedViewerContext viewer,
        bool viewerIsWoman,
        bool viewerWantsWomenOnly,
        bool viewerWantsVerifiedOnly,
        bool viewerWantsOutsideCountry,
        string? viewerCountry,
        IReadOnlyCollection<Guid> seen)
    {
        var query = dbContext.Profiles
            .AsNoTracking()
            .Where(c => c.Status == "active")          // not draft/paused/under_review/banned/deleted
            .Where(c => c.UserId != viewerUserId)      // never show the viewer themselves
            .Where(c => !dbContext.VisibilityPreferences.Any(v => v.ProfileId == c.Id && v.Paused))
            // blocks honoured in both directions (§6.5)
            .Where(c => !dbContext.UserBlocks.Any(b =>
                (b.BlockerUserId == viewerUserId && b.BlockedUserId == c.UserId) ||
                (b.BlockerUserId == c.UserId && b.BlockedUserId == viewerUserId)))
            // recency: already-appreciated profiles drop out for good (§7.6)
            .Where(c => !dbContext.AppreciationEvents.Any(a =>
                a.SenderUserId == viewerUserId && a.ReceiverProfileId == c.Id))
            // a profile only appears once it has a ready photo to show (§6.3)
            .Where(c => dbContext.Photos.Any(ph => ph.ProfileId == c.Id && ph.Status == "ready"));

        if (seen.Count > 0)
        {
            // session-level dedupe of recently-shown profiles (§7.6)
            query = query.Where(c => !seen.Contains(c.Id));
        }

        // Audience: women-for-women is bidirectional (§7.3). A viewer in the mode
        // sees only women; a candidate in the mode is shown only to women viewers.
        if (viewerWantsWomenOnly)
        {
            query = query.Where(c => c.Gender == "woman");
        }

        if (!viewerIsWoman)
        {
            query = query.Where(c => !dbContext.VisibilityPreferences.Any(v => v.ProfileId == c.Id && v.WomenForWomen));
        }

        // Verified-only is likewise bidirectional: a viewer who wants verified-only
        // sees only verified profiles; a verified-only candidate is hidden from
        // unverified viewers.
        if (viewerWantsVerifiedOnly)
        {
            query = query.Where(c => c.Verified);
        }

        if (!viewer.Verified)
        {
            query = query.Where(c => !dbContext.VisibilityPreferences.Any(v => v.ProfileId == c.Id && v.VerifiedOnly));
        }

        // Country rules (§7.3, §10.4). Distance (min_distance_km) is intentionally
        // not applied yet: it needs the coarse geopoint, which is captured with
        // consent in M8 — there is no coordinate to measure against in M4. The
        // toggle and column already exist, so adding the Haversine predicate later
        // is a localized change here.
        if (viewerWantsOutsideCountry && viewerCountry is not null)
        {
            query = query.Where(c => c.CountryCode != viewerCountry);
        }

        if (viewerCountry is not null)
        {
            query = query.Where(c => !(
                dbContext.VisibilityPreferences.Any(v => v.ProfileId == c.Id && v.HideFromCountry) &&
                c.CountryCode == viewerCountry));
        }

        return query.Select(c => c.Id);
    }
}
