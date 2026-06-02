using Hpn.Modules.Appreciation.Contracts.Dtos;
using Hpn.SharedKernel.Math;

namespace Hpn.Modules.SocialFingerprint.Internal.Features.GetMyFingerprint;

internal static class FingerprintDistribution
{
    public const int MinimumSampleSize = 20;

    public static IReadOnlyCollection<FingerprintDistributionItemResponse> Build(
        ReceivedAppreciationSummaryDto summary,
        IReadOnlyCollection<AppreciationCategoryDto> activeCategories)
    {
        var counts = summary.Categories.ToDictionary(c => c.CategoryId, c => c.Count);

        return activeCategories
            .OrderBy(c => c.SortOrder)
            .Select(c =>
            {
                var count = counts.GetValueOrDefault(c.Id);
                return new FingerprintDistributionItemResponse(
                    c.Id,
                    c.Slug,
                    c.Label,
                    RoundShare(count, summary.Total),
                    FingerprintPhrasing.ForCategory(c.Slug, c.Label));
            })
            .ToArray();
    }

    public static IReadOnlyCollection<FingerprintTraitResponse> TopTraits(
        ReceivedAppreciationSummaryDto summary,
        IReadOnlyCollection<AppreciationCategoryDto> activeCategories,
        int limit = 3)
    {
        var sortOrders = activeCategories.ToDictionary(c => c.Id, c => c.SortOrder);

        return summary.Categories
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .ThenBy(c => sortOrders.GetValueOrDefault(c.CategoryId, int.MaxValue))
            .Take(limit)
            .Select(c => new FingerprintTraitResponse(
                c.CategoryId,
                c.Slug,
                c.Label,
                RoundShare(c.Count, summary.Total),
                FingerprintPhrasing.ForTrait(c.Slug, c.Label)))
            .ToArray();
    }

    internal static double RoundShare(int count, int total) => ShareMath.Round(count, total);
}
