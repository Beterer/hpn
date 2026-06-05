namespace Hpn.Modules.Photo.Internal.Domain;

internal sealed class ProfilePhoto
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public short Position { get; private set; }
    public bool IsPrimary { get; private set; }
    public PhotoStatus Status { get; private set; }
    public string OriginalKey { get; private set; } = null!;
    public string DisplayKey { get; private set; } = null!;
    public string ThumbKey { get; private set; } = null!;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string ContentHash { get; private set; } = null!;
    public string? ScanResult { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ProfilePhoto()
    {
    }

    public static ProfilePhoto CreateReady(
        Guid id,
        Guid profileId,
        int position,
        string originalKey,
        string displayKey,
        string thumbKey,
        int width,
        int height,
        string contentHash,
        string? scanResult,
        DateTimeOffset now,
        bool isPrimary = false)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Photo position cannot be negative.");
        }

        return new ProfilePhoto
        {
            Id = id,
            ProfileId = profileId,
            Position = checked((short)position),
            IsPrimary = isPrimary,
            Status = PhotoStatus.Ready,
            OriginalKey = originalKey,
            DisplayKey = displayKey,
            ThumbKey = thumbKey,
            Width = width,
            Height = height,
            ContentHash = contentHash,
            ScanResult = scanResult,
            CreatedAt = now,
        };
    }

    public void MoveTo(int position)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Photo position cannot be negative.");
        }

        Position = checked((short)position);
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }
}
