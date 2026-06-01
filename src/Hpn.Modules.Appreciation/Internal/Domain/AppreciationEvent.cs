namespace Hpn.Modules.Appreciation.Internal.Domain;

/// <summary>
/// One positive, categorized appreciation: a sender notices a quality on a
/// receiver's profile (backbone §7.5). The full write path — validation,
/// idempotency, counter/style projections in one transaction — lands in M5.
/// M4 introduces the table only so the Feed eligibility read model can exclude
/// already-appreciated profiles (§6.5, §7.6).
/// </summary>
internal sealed class AppreciationEvent
{
    public Guid Id { get; private set; }
    public Guid SenderUserId { get; private set; }
    public Guid ReceiverProfileId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid? PhotoId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AppreciationEvent()
    {
    }

    public static AppreciationEvent Create(
        Guid senderUserId,
        Guid receiverProfileId,
        Guid categoryId,
        Guid? photoId,
        DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        SenderUserId = senderUserId,
        ReceiverProfileId = receiverProfileId,
        CategoryId = categoryId,
        PhotoId = photoId,
        CreatedAt = now,
    };
}
