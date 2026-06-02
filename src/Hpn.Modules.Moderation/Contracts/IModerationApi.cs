namespace Hpn.Modules.Moderation.Contracts;

/// <summary>
/// The surface other modules may call into Moderation through (backbone §6.7,
/// §3.3). Member reports are filed via the HTTP endpoint; the Admin module uses
/// the explicit decision method for human moderation actions so it never reaches
/// into Moderation internals.
/// </summary>
public interface IModerationApi
{
    /// <summary>The cached trust score for an account, in [0,1] (§10.3). Defaults to the
    /// base score when the account has never been scored.</summary>
    Task<double> GetTrustScoreAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Whether the account is currently excluded by moderation — an active
    /// temporary restriction or a ban that has not been cleared (§10.3).</summary>
    Task<bool> IsRestrictedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Applies an admin moderation decision to a profile owner. Action is one
    /// of <c>warn</c>, <c>temp_restrict</c>, <c>ban</c>, <c>clear</c>.</summary>
    Task<ModerationDecisionDto> ApplyAdminProfileActionAsync(
        Guid targetProfileId,
        Guid targetUserId,
        string action,
        string reason,
        Guid adminUserId,
        CancellationToken cancellationToken = default);
}

public sealed record ModerationDecisionDto(
    Guid TargetProfileId,
    Guid TargetUserId,
    string Action,
    DateTimeOffset? ExpiresAt);
