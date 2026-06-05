namespace Hpn.Modules.Notification.Internal.Features.GetNotificationSummary;

internal sealed record GetNotificationSummaryResponse(
    int UnseenCount,
    NotificationItemResponse? Latest);

internal sealed record NotificationItemResponse(
    Guid Id,
    string Type,
    string TraitLabel,
    string CategorySlug,
    DateTimeOffset CreatedAt,
    bool Seen);
