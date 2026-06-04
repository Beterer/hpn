using Hpn.SharedKernel.Events;

namespace Hpn.SharedKernel.Accounts;

public sealed record GuestConverted(Guid GuestId, Guid UserId, DateTimeOffset OccurredAt) : IDomainEvent;
