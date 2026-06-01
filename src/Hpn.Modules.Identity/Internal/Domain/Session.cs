namespace Hpn.Modules.Identity.Internal.Domain;

/// <summary>
/// An opaque, server-side session (backbone §10.1). The cookie carries a random
/// secret; only its hash is stored here so a DB leak can't be replayed. Sliding
/// expiry, revocable at any time (logout, deletion, ban).
/// </summary>
internal sealed class Session
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Ip { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private Session()
    {
    }

    public static Session Start(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime, string? userAgent, string? ip) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + lifetime,
        UserAgent = userAgent,
        Ip = ip,
    };

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;

    /// <summary>
    /// Extends the window when the session is past the halfway point, so a steady
    /// user stays signed in without a DB write on every request.
    /// </summary>
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
