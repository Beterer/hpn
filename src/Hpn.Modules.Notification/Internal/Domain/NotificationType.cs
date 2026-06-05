namespace Hpn.Modules.Notification.Internal.Domain;

internal enum NotificationType
{
    AppreciationReceived,
}

internal static class NotificationTypeFormat
{
    public static string ToStorageValue(NotificationType type) => type switch
    {
        NotificationType.AppreciationReceived => "appreciation_received",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown notification type."),
    };

    public static NotificationType Parse(string value) => value switch
    {
        "appreciation_received" => NotificationType.AppreciationReceived,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown notification type."),
    };
}
