namespace Hpn.Modules.Appreciation.Internal.Domain;

internal sealed class AppreciationEvent
{
    public Guid Id { get; private set; }
    public Guid SenderUserId { get; private set; }
    public Guid ReceiverProfileId { get; private set; }

    // CategoryId stays denormalized from the trait (ADR-025) so the category-level
    // projections, the (sender, receiver, category) duplicate guard, and the
    // GuestConverted re-key SQL keep working without a join.
    public Guid CategoryId { get; private set; }
    public Guid TraitId { get; private set; }
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
        Guid traitId,
        Guid? photoId,
        string idempotencyKey,
        DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        SenderUserId = senderUserId,
        ReceiverProfileId = receiverProfileId,
        CategoryId = categoryId,
        TraitId = traitId,
        PhotoId = photoId,
        IdempotencyKey = NormalizeIdempotencyKey(idempotencyKey),
        CreatedAt = now,
    };

    public bool MatchesRequest(Guid receiverProfileId, Guid traitId, Guid? photoId) =>
        ReceiverProfileId == receiverProfileId &&
        TraitId == traitId &&
        PhotoId == photoId;

    private static string NormalizeIdempotencyKey(string value) => value.Trim();
}
