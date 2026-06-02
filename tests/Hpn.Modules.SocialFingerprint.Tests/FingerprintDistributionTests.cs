using FluentAssertions;
using Hpn.Modules.Appreciation.Contracts.Dtos;
using Hpn.Modules.SocialFingerprint.Internal.Features.GetMyFingerprint;
using Xunit;

namespace Hpn.Modules.SocialFingerprint.Tests;

public sealed class FingerprintDistributionTests
{
    [Fact]
    public void Builds_distribution_and_traits_from_received_stats_without_ranking_copy()
    {
        var warmSmile = new AppreciationCategoryDto(
            new Guid("0f93fb39-2e34-4c90-bf9d-28df31447201"),
            "warm_smile",
            "Warm smile",
            1);
        var creative = new AppreciationCategoryDto(
            new Guid("0f93fb39-2e34-4c90-bf9d-28df31447210"),
            "creative",
            "Creative",
            10);
        var summary = new ReceivedAppreciationSummaryDto(
            Guid.NewGuid(),
            20,
            [
                new AppreciationCategoryCountDto(warmSmile.Id, warmSmile.Slug, warmSmile.Label, 14),
                new AppreciationCategoryCountDto(creative.Id, creative.Slug, creative.Label, 6),
            ]);

        var distribution = FingerprintDistribution.Build(summary, [warmSmile, creative]).ToArray();
        var topTraits = FingerprintDistribution.TopTraits(summary, [warmSmile, creative]).ToArray();

        distribution.Should().HaveCount(2);
        distribution[0].Share.Should().Be(0.7);
        distribution[1].Share.Should().Be(0.3);
        topTraits.Select(t => t.Slug).Should().Equal("warm_smile", "creative");
        topTraits[0].Phrasing.Should().StartWith("Warmth");

        var copy = string.Join(' ', distribution.Select(d => d.Phrasing).Concat(topTraits.Select(t => t.Phrasing)));
        copy.Should().NotContain("score");
        copy.Should().NotContain("rank");
        copy.Should().NotContain("leaderboard");
        copy.Should().NotContain("popular");
    }

    [Fact]
    public void Weekly_period_starts_on_monday_in_utc()
    {
        var sunday = new DateTimeOffset(2026, 6, 7, 23, 30, 0, TimeSpan.Zero);

        GetMyFingerprintHandler.GetWeekStart(sunday).Should().Be(new DateOnly(2026, 6, 1));
    }
}
