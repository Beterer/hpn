namespace Hpn.Modules.Moderation.Internal.Trust;

/// <summary>
/// The raw inputs to the trust score (backbone §10.3), gathered from other modules'
/// contracts plus this module's own actions. Persisted alongside the score as the
/// <c>signals</c> JSON so a score is always explainable and the constants are tunable.
/// </summary>
internal sealed record TrustSignals(
    double AccountAgeDays,
    bool HasReadyPrimaryPhoto,
    bool Verified,
    long GivenAppreciations,
    long ReceivedAppreciations,
    int UpheldActions);
