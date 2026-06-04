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

    // The specific recurring traits (ADR-025): top named traits by how often people
    // chose them, coloured by their category's hue. The radar above stays
    // category-level; this is the second level of the taxonomy.
    public static IReadOnlyCollection<FingerprintTraitResponse> TopTraits(
        IReadOnlyCollection<AppreciationTraitCountDto> traits,
        int total,
        int limit = 3)
    {
        return traits
            .Where(t => t.Count > 0)
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Slug)
            .Take(limit)
            .Select(t => new FingerprintTraitResponse(
                t.TraitId,
                t.Slug,
                t.Label,
                RoundShare(t.Count, total),
                FingerprintPhrasing.ForTrait(t.Label),
                t.Hue))
            .ToArray();
    }

    internal static double RoundShare(int count, int total) => ShareMath.Round(count, total);
}
