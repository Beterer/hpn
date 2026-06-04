namespace Hpn.Modules.Identity.Internal.Domain;

/// <summary>
/// Opaque, revocable actor session for a visitor who has not created an account.
/// Guests are actors, not users: this id must never be projected as a user id.
/// </summary>
internal sealed class GuestSession
{
    public Guid Id { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Ip { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ConvertedToUserId { get; private set; }
    public DateTimeOffset? ConvertedAt { get; private set; }

    private GuestSession()
    {
    }

    public static GuestSession Start(
        string tokenHash,
        DateTimeOffset now,
        TimeSpan lifetime,
        string? userAgent,
        string? ip) => new()
    {
        Id = Guid.CreateVersion7(),
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + lifetime,
        UserAgent = userAgent,
        Ip = ip,
    };

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }

    public void ConvertTo(Guid userId, DateTimeOffset now)
    {
        ConvertedToUserId ??= userId;
        ConvertedAt ??= now;
        Revoke(now);
    }

    public bool SlideIfDue(DateTimeOffset now, TimeSpan lifetime)
    {
        var remaining = ExpiresAt - now;
        if (remaining < lifetime / 2)
        {
            ExpiresAt = now + lifetime;
            return true;
        }

        return false;
    }
}
