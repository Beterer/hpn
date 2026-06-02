using FluentAssertions;
using Hpn.Modules.Moderation.Internal.Trust;
using Xunit;

namespace Hpn.Modules.Moderation.Tests;

/// <summary>
/// The trust-score formula (backbone §10.3). Constants are launch defaults; these
/// tests pin the shape (each input's contribution, ramps, penalty, clamping) so a
/// tuning change is a deliberate edit, not an accident.
/// </summary>
public sealed class TrustScoreCalculatorTests
{
    private static TrustSignals Signals(
        double ageDays = 0,
        bool photo = false,
        bool verified = false,
        long given = 0,
        long received = 0,
        int upheld = 0) =>
        new(ageDays, photo, verified, given, received, upheld);

    [Fact]
    public void Brand_new_bare_account_scores_the_base()
    {
        TrustScoreCalculator.Compute(Signals()).Should().Be(0.4);
    }

    [Fact]
    public void Account_age_ramps_linearly_over_two_weeks()
    {
        TrustScoreCalculator.Compute(Signals(ageDays: 0)).Should().Be(0.4);
        TrustScoreCalculator.Compute(Signals(ageDays: 7)).Should().BeApproximately(0.5, 1e-9);
        TrustScoreCalculator.Compute(Signals(ageDays: 14)).Should().BeApproximately(0.6, 1e-9);
        // The age bonus is capped — older than the ramp adds nothing more.
        TrustScoreCalculator.Compute(Signals(ageDays: 365)).Should().BeApproximately(0.6, 1e-9);
    }

    [Fact]
    public void A_ready_primary_photo_adds_its_weight()
    {
        TrustScoreCalculator.Compute(Signals(photo: true)).Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void Verification_adds_its_weight()
    {
        TrustScoreCalculator.Compute(Signals(verified: true)).Should().BeApproximately(0.6, 1e-9);
    }

    [Fact]
    public void Engagement_needs_both_directions_above_the_threshold()
    {
        TrustScoreCalculator.Compute(Signals(given: 10, received: 9)).Should().Be(0.4);
        TrustScoreCalculator.Compute(Signals(given: 9, received: 10)).Should().Be(0.4);
        TrustScoreCalculator.Compute(Signals(given: 10, received: 10)).Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void A_fully_trusted_account_clamps_at_one()
    {
        // base .4 + age .2 + photo .1 + verified .2 + engagement .1 = 1.0
        var score = TrustScoreCalculator.Compute(
            Signals(ageDays: 30, photo: true, verified: true, given: 50, received: 50));
        score.Should().Be(1.0);
    }

    [Fact]
    public void Upheld_actions_subtract_and_the_score_never_goes_negative()
    {
        // base .4 − .25 = .15
        TrustScoreCalculator.Compute(Signals(upheld: 1)).Should().BeApproximately(0.15, 1e-9);
        // base .4 − .50 → clamped to 0
        TrustScoreCalculator.Compute(Signals(upheld: 2)).Should().Be(0.0);
        TrustScoreCalculator.Compute(Signals(upheld: 10)).Should().Be(0.0);
    }
}
