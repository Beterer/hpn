using Hpn.Modules.Appreciation.Internal.Persistence;
using Hpn.SharedKernel.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Hpn.Modules.Appreciation.Internal.Features.GetAppreciationStyle;

internal sealed class GetAppreciationStyleHandler(
    AppreciationDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache cache)
{
    // The platform-wide totals are the same for everyone and change slowly, so we
    // compute them once and reuse for a few minutes instead of scanning every
    // user's rows on each request. A brand-new appreciation shows up in the
    // comparison within this window — fine for a "vs. the wider pattern" reading.
    private const string PlatformCountsCacheKey = "appreciation-style:platform-counts";
    private static readonly TimeSpan PlatformCountsTtl = TimeSpan.FromMinutes(5);

    public async Task<GetAppreciationStyleResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var categories = await dbContext.AppreciationCategories
            .AsNoTracking()
            .Where(c => c.Active)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.Slug,
                c.Label,
            })
            .ToArrayAsync(cancellationToken);

        var userCounts = await dbContext.GivenAppreciationStats
            .AsNoTracking()
            .Where(s => s.SenderUserId == userId)
            .ToDictionaryAsync(s => s.CategoryId, s => s.Count, cancellationToken);

        var platformCounts = await GetPlatformCountsAsync(cancellationToken);

        var userTotal = userCounts.Values.Sum();
        var platformTotal = platformCounts.Values.Sum();
        var responseCategories = categories
            .Select(category =>
            {
                var count = userCounts.GetValueOrDefault(category.Id);
                var userShare = AppreciationStyleMath.Share(count, userTotal);
                var platformShare = AppreciationStyleMath.Share(
                    platformCounts.GetValueOrDefault(category.Id),
                    platformTotal);
                var difference = AppreciationStyleMath.Difference(userShare, platformShare);

                return new AppreciationStyleCategoryResponse(
                    category.Id,
                    category.Slug,
                    category.Label,
                    count,
                    userShare,
                    platformShare,
                    difference,
                    AppreciationStylePhrasing.ForCategory(category.Label, count, difference));
            })
            .ToArray();

        return new GetAppreciationStyleResponse(
            userId,
            userTotal == 0 ? "empty" : "ready",
            AppreciationStylePhrasing.Headline,
            userTotal == 0 ? AppreciationStylePhrasing.EmptySummary : AppreciationStylePhrasing.Summary,
            userTotal,
            responseCategories);
    }

    private async Task<IReadOnlyDictionary<Guid, int>> GetPlatformCountsAsync(CancellationToken cancellationToken)
    {
        var counts = await cache.GetOrCreateAsync(PlatformCountsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PlatformCountsTtl;
            return await dbContext.GivenAppreciationStats
                .AsNoTracking()
                .GroupBy(s => s.CategoryId)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    Count = g.Sum(s => s.Count),
                })
                .ToDictionaryAsync(s => s.CategoryId, s => s.Count, cancellationToken);
        });

        return counts ?? new Dictionary<Guid, int>();
    }
}
