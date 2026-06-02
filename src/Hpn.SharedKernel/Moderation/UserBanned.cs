using Hpn.SharedKernel.Events;

namespace Hpn.SharedKernel.Moderation;

/// <summary>
/// Raised when an account is banned (backbone §6.7). A ban is always an
/// admin/system decision recorded in <c>moderation_actions</c> — it is never
/// applied automatically from report pressure. Profile moves the account to
/// <c>banned</c> so it can never reappear in the feed.
/// </summary>
public sealed record UserBanned(Guid UserId, DateTimeOffset OccurredAt) : IDomainEvent;
