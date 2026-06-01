using FluentAssertions;
using Hpn.Modules.Feed.Internal.Ranking;
using Xunit;

namespace Hpn.Modules.Feed.Tests;

/// <summary>
/// The v1 ranking strategy (backbone §6.5): it must select <em>within</em> the
/// eligible set only, respect the batch size, and never invent or drop ids.
/// </summary>
public sealed class RandomWithinEligibleStrategyTests
{
    private static readonly FeedViewerContext Viewer =
        new(Guid.NewGuid(), Guid.NewGuid(), "woman", "RO", Verified: false);

    private readonly RandomWithinEligibleStrategy _strategy = new();

    [Fact]
    public void Selects_only_from_the_eligible_set_and_respects_batch_size()
    {
        var eligible = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();

        var selected = _strategy.Select(eligible, Viewer, batchSize: 3);

        selected.Should().HaveCount(3);
        selected.Should().OnlyHaveUniqueItems();
        selected.Should().BeSubsetOf(eligible);
    }

    [Fact]
    public void Returns_a_permutation_when_batch_is_at_least_the_eligible_count()
    {
        var eligible = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();

        var selected = _strategy.Select(eligible, Viewer, batchSize: 50);

        selected.Should().BeEquivalentTo(eligible);
    }

    [Fact]
    public void Returns_empty_for_no_candidates_or_zero_batch()
    {
        _strategy.Select([], Viewer, batchSize: 5).Should().BeEmpty();
        _strategy.Select([Guid.NewGuid()], Viewer, batchSize: 0).Should().BeEmpty();
    }
}
