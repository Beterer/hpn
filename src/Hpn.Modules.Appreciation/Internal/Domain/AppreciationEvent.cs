namespace Hpn.Modules.Appreciation.Internal.Domain;

internal sealed class AppreciationEvent
{
    public Guid Id { get; private set; }
    public Guid SenderUserId { get; private set; }
    public Guid ReceiverProfileId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? PhotoId { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private AppreciationEvent()
    {
    }

    public static AppreciationEvent Create(
        Guid senderUserId,
        Guid receiverProfileId,
        Guid categoryId,
        Guid? photoId,
        string idempotencyKey,
        DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        SenderUserId = senderUserId,
        ReceiverProfileId = receiverProfileId,
        CategoryId = categoryId,
        PhotoId = photoId,
        IdempotencyKey = NormalizeIdempotencyKey(idempotencyKey),
        CreatedAt = now,
    };

    public bool MatchesRequest(Guid receiverProfileId, Guid categoryId, Guid? photoId) =>
        ReceiverProfileId == receiverProfileId &&
        CategoryId == categoryId &&
        PhotoId == photoId;

    private static string NormalizeIdempotencyKey(string value) => value.Trim();
}
