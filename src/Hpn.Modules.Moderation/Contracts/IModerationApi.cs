namespace Hpn.Modules.Moderation.Contracts;

/// <summary>
/// The read-only surface other modules (notably Admin in M10) may call into
/// Moderation through (backbone §6.7, §3.3). Reports are filed via the HTTP endpoint,
/// and restrictions/bans are applied inside the module's own services — writes never
/// leak onto this contract, consistent with the other modules' read-only APIs.
/// </summary>
public interface IModerationApi
{
    /// <summary>The cached trust score for an account, in [0,1] (§10.3). Defaults to the
    /// base score when the account has never been scored.</summary>
    Task<double> GetTrustScoreAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Whether the account is currently excluded by moderation — an active
    /// temporary restriction or a ban that has not been cleared (§10.3).</summary>
    Task<bool> IsRestrictedAsync(Guid userId, CancellationToken cancellationToken = default);
}
