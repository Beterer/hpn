namespace Hpn.Modules.Moderation.Contracts;

/// <summary>
/// Launch-default moderation constants shared across modules (backbone §10.3). The
/// base trust score is the value an account scores before any signals, and the value
/// used wherever an account has no cached <c>account_trust</c> row yet.
/// </summary>
public static class ModerationDefaults
{
    /// <summary>The starting trust score, in [0,1] (§10.3). Single source of truth so
    /// the calculator, the contract default, and admin read models can't drift.</summary>
    public const double BaseTrustScore = 0.4;
}
