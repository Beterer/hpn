using FluentAssertions;
using Hpn.Modules.Appreciation.Internal.Domain;
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
}
