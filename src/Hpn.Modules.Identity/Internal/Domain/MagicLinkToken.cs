namespace Hpn.Modules.Identity.Internal.Domain;

/// <summary>
/// A single-use, hashed sign-in token (backbone §7.2, §10.1). Only the hash is
/// stored — the raw token lives solely in the emailed link. Expires in ~15 min
/// and is consumed on first successful verification.
/// </summary>
internal sealed class MagicLinkToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? RequestedIp { get; private set; }

    private MagicLinkToken()
    {
    }

    public static MagicLinkToken Issue(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime, string? requestedIp) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + lifetime,
        RequestedIp = requestedIp,
    };

    public bool IsRedeemable(DateTimeOffset now) => ConsumedAt is null && now < ExpiresAt;

    public void Consume(DateTimeOffset now) => ConsumedAt = now;
}
