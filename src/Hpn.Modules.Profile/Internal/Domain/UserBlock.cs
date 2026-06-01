namespace Hpn.Modules.Profile.Internal.Domain;

internal sealed class UserBlock
{
    public Guid BlockerUserId { get; private set; }
    public Guid BlockedUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private UserBlock()
    {
    }

    public static UserBlock Create(Guid blockerUserId, Guid blockedUserId, DateTimeOffset now) => new()
    {
        BlockerUserId = blockerUserId,
        BlockedUserId = blockedUserId,
        CreatedAt = now,
    };
}
