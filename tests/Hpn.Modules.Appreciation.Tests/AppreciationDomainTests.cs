using FluentAssertions;
using Hpn.Modules.Appreciation.Internal.Domain;
using Hpn.Modules.Appreciation.Internal.Features.GetAppreciationStyle;
using Hpn.Modules.Appreciation.Internal.Features.GetReceivedAppreciation;
using Xunit;

namespace Hpn.Modules.Appreciation.Tests;

public sealed class AppreciationDomainTests
{
    [Fact]
    public void Seed_contains_the_fixed_twelve_positive_categories()
    {
        AppreciationCategorySeed.All.Should().HaveCount(12);
        AppreciationCategorySeed.All.Select(c => c.Slug).Should().Equal(
            "warm_smile",
            "authentic",
            "stylish",
            "calming_energy",
            "confident",
            "expressive",
            "fun_energy",
            "elegant",
            "trustworthy",
            "creative",
            "kind",
            "intelligent");
        AppreciationCategorySeed.All.Select(c => c.SortOrder).Should().Equal(Enumerable.Range(1, 12));
    }

    [Fact]
    public void Appreciation_event_trims_idempotency_key_and_matches_original_request()
    {
        var sender = Guid.NewGuid();
        var receiver = Guid.NewGuid();
        var category = Guid.NewGuid();
        var photo = Guid.NewGuid();

        var appreciation = AppreciationEvent.Create(
            sender,
            receiver,
            category,
            photo,
            " retry-key ",
            DateTimeOffset.UtcNow);

        appreciation.IdempotencyKey.Should().Be("retry-key");
        appreciation.MatchesRequest(receiver, category, photo).Should().BeTrue();
        appreciation.MatchesRequest(receiver, Guid.NewGuid(), photo).Should().BeFalse();
    }

    [Fact]
    public void Received_appreciation_phrasing_stays_perception_based_without_ranking_language()
    {
        var curated = ReceivedAppreciationPhrasing.ForCategory("kind", "Kind");
        // An unseeded slug exercises the generic fallback template.
        var fallback = ReceivedAppreciationPhrasing.ForCategory("graceful", "Graceful");
        var eventPhrase = ReceivedAppreciationPhrasing.ForEvent("warm_smile", "Warm smile");

        curated.Should().StartWith("People often");
        fallback.Should().StartWith("People often describe you as graceful");
        eventPhrase.Should().StartWith("Someone noticed");
        var joined = string.Join(' ', ReceivedAppreciationPhrasing.Headline, curated, fallback, eventPhrase);
        joined.Should().NotContain("score");
        joined.Should().NotContain("rank");
        joined.Should().NotContain("leaderboard");
        joined.Should().NotContain("popular");
    }

    [Fact]
    public void Appreciation_style_math_and_phrasing_stay_interpretive()
    {
        var userShare = AppreciationStyleMath.Share(count: 3, total: 4);
        var platformShare = AppreciationStyleMath.Share(count: 3, total: 8);
        var difference = AppreciationStyleMath.Difference(userShare, platformShare);

        userShare.Should().Be(0.75);
        platformShare.Should().Be(0.375);
        difference.Should().Be(0.375);

        var insight = AppreciationStylePhrasing.ForCategory("Warm smile", count: 3, difference);
        insight.Should().Contain("wider Notice pattern");
        insight.Should().NotContain("score");
        insight.Should().NotContain("rank");
        insight.Should().NotContain("leaderboard");
        insight.Should().NotContain("popular");
    }
}
