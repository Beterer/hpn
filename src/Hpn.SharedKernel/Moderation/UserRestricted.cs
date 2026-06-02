using Hpn.SharedKernel.Events;

namespace Hpn.SharedKernel.Moderation;

/// <summary>
/// Raised when a temporary restriction is placed on an account (backbone §6.7,
/// §10.3) — always temporary, never an automatic ban. Modules that must make the
/// account inert handle this: Profile moves it to <c>under_review</c> so every
/// feed eligibility query drops it at once. Lives in the shared kernel (not the
/// Moderation module's Contracts) so Profile can subscribe without taking a
/// dependency on Moderation — the same shape as
/// <see cref="Accounts.AccountDeletionRequested"/>.
/// </summary>
public sealed record UserRestricted(Guid UserId, DateTimeOffset ExpiresAt, DateTimeOffset OccurredAt) : IDomainEvent;
