namespace Hpn.Modules.Moderation.Internal.Domain;

/// <summary>
/// An audit record of a moderation decision against an account (backbone §7.8). Every
/// restriction, ban and clear is one of these rows — bans and clears <em>only</em>
/// happen this way (§10.3, never a side effect elsewhere). <see cref="Actor"/> is the
/// literal <c>"system"</c> for automatic restrictions, or the admin user id (as a
/// string) for human decisions. <see cref="ExpiresAt"/> is set only for
/// <see cref="ActionType.TempRestrict"/>.
/// </summary>
internal sealed class ModerationAction
{
    public const string SystemActor = "system";

    public Guid Id { get; private set; }
    public Guid TargetUserId { get; private set; }
    public ActionType Action { get; private set; }
    public string Reason { get; private set; } = null!;
    public string Actor { get; private set; } = null!;
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ModerationAction()
    {
    }

    public static ModerationAction Warn(Guid targetUserId, string reason, string actor, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        TargetUserId = targetUserId,
        Action = ActionType.Warn,
        Reason = reason,
        Actor = actor,
        CreatedAt = now,
    };

    public static ModerationAction TempRestrict(
        Guid targetUserId,
        string reason,
        string actor,
        DateTimeOffset now,
        DateTimeOffset expiresAt) => new()
    {
        Id = Guid.CreateVersion7(),
        TargetUserId = targetUserId,
        Action = ActionType.TempRestrict,
        Reason = reason,
        Actor = actor,
        ExpiresAt = expiresAt,
        CreatedAt = now,
    };

    public static ModerationAction Ban(Guid targetUserId, string reason, string actor, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        TargetUserId = targetUserId,
        Action = ActionType.Ban,
        Reason = reason,
        Actor = actor,
        CreatedAt = now,
    };

    public static ModerationAction Clear(Guid targetUserId, string reason, string actor, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        TargetUserId = targetUserId,
        Action = ActionType.Clear,
        Reason = reason,
        Actor = actor,
        CreatedAt = now,
    };

    public bool IsActiveRestrictionAt(DateTimeOffset now) =>
        Action == ActionType.TempRestrict && ExpiresAt is { } expires && expires > now;
}
