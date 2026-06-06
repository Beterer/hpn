namespace Hpn.Modules.Notification.Internal.Domain;

/// <summary>
/// A delivered signal for a member. It deliberately stores what was appreciated,
/// never who sent the appreciation.
/// </summary>
internal sealed class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public Guid SourceId { get; private set; }
    public string TraitLabel { get; private set; } = null!;
    public string CategorySlug { get; private set; } = null!;

    // Natural, receiver-facing sentence to display ("Someone felt your good vibe.").
    // Snapshotted from the source event so the read path needs no copy logic.
    public string Phrasing { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SeenAt { get; private set; }

    private Notification()
    {
    }
}
