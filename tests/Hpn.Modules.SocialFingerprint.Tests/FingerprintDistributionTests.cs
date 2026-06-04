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
        var physical = new AppreciationCategoryDto(
            new Guid("0f93fb39-2e34-4c90-bf9d-28df31447301"),
            "physical",
            "Physical",
            1,
            38,
            []);
        var mind = new AppreciationCategoryDto(
            new Guid("0f93fb39-2e34-4c90-bf9d-28df31447305"),
            "mind",
            "Mind",
            5,
            264,
            []);
        var summary = new ReceivedAppreciationSummaryDto(
            Guid.NewGuid(),
            20,
            [
                new AppreciationCategoryCountDto(physical.Id, physical.Slug, physical.Label, 14),
                new AppreciationCategoryCountDto(mind.Id, mind.Slug, mind.Label, 6),
            ]);

        // Trait-level recurring traits (ADR-025) come from per-trait counts, coloured
        // by their category's hue; the radar/distribution stays category-level.
        var traitCounts = new[]
        {
            new AppreciationTraitCountDto(Guid.NewGuid(), "warm_smile", "Warm smile", "physical", 38, 14),
            new AppreciationTraitCountDto(Guid.NewGuid(), "good_vibe", "Good vibe", "energy", 78, 6),
        };

        var distribution = FingerprintDistribution.Build(summary, [physical, mind]).ToArray();
        var topTraits = FingerprintDistribution.TopTraits(traitCounts, total: 20).ToArray();

        distribution.Should().HaveCount(2);
        distribution[0].Share.Should().Be(0.7);
        distribution[1].Share.Should().Be(0.3);
        topTraits.Select(t => t.Slug).Should().Equal("warm_smile", "good_vibe");
        topTraits[0].Share.Should().Be(0.7);
        topTraits[0].Hue.Should().Be(38);
        topTraits[0].Phrasing.Should().Contain("perceive");

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
