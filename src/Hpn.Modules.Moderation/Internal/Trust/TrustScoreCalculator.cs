namespace Hpn.Modules.Moderation.Internal.Trust;

/// <summary>
/// The trust-score formula (backbone §10.3), isolated as a pure function so the
/// numeric constants — explicitly launch defaults to tune with real data — are easy
/// to test and to change without touching the gathering/persistence around it.
/// </summary>
internal static class TrustScoreCalculator
{
    public const double Base = 0.4;
    public const double AgeWeight = 0.2;
    public const int AgeRampDays = 14;
    public const double PhotoWeight = 0.1;
    public const double VerifiedWeight = 0.2;
    public const double EngagementWeight = 0.1;
    public const int EngagementThreshold = 10;
    public const double UpheldActionPenalty = 0.25;

    /// <summary>Computes the [0,1] trust score from its signals.</summary>
    public static double Compute(TrustSignals signals)
    {
        var score = Base;

        // Account age ramps the bonus linearly over the first two weeks.
        var ageFraction = Math.Clamp(signals.AccountAgeDays / AgeRampDays, 0.0, 1.0);
        score += AgeWeight * ageFraction;

        if (signals.HasReadyPrimaryPhoto)
        {
            score += PhotoWeight;
        }

        if (signals.Verified)
        {
            score += VerifiedWeight;
        }

        // Genuine engagement: meaningfully active in both directions.
        if (signals.GivenAppreciations >= EngagementThreshold &&
            signals.ReceivedAppreciations >= EngagementThreshold)
        {
            score += EngagementWeight;
        }

        score -= UpheldActionPenalty * signals.UpheldActions;

        return Math.Clamp(score, 0.0, 1.0);
    }
}
