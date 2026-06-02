using Hpn.SharedKernel.Events;

namespace Hpn.SharedKernel.Accounts;

/// <summary>
/// Raised the moment a member asks to delete their account (the soft-delete phase,
/// backbone §10.5). Modules that need to make the account inert immediately —
/// notably Profile, which hides it from the feed — handle this. The irreversible
/// purge happens later via <see cref="IAccountDataContributor.EraseAsync"/>. Lives
/// in the shared kernel (not a module's Contracts) so any module can subscribe
/// without taking a dependency on Identity.
/// </summary>
public sealed record AccountDeletionRequested(Guid UserId, DateTimeOffset OccurredAt) : IDomainEvent;
