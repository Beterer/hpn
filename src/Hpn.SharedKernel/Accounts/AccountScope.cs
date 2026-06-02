namespace Hpn.SharedKernel.Accounts;

/// <summary>
/// Identifies the account being exported or erased. <see cref="ProfileId"/> is
/// resolved once by the orchestrator and passed to every contributor, so a
/// contributor never has to re-resolve it (and erase ordering can't race the
/// profile row's own deletion). It is null when the user never created a profile.
/// </summary>
public sealed record AccountScope(Guid UserId, Guid? ProfileId);
