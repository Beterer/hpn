namespace Hpn.Modules.SocialFingerprint.Internal.Domain;

internal sealed class SocialFingerprintSnapshot
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public string Period { get; private set; } = null!;
    public DateOnly PeriodStart { get; private set; }
    public int SampleSize { get; private set; }
    public string Distribution { get; private set; } = null!;
    public string TopTraits { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private SocialFingerprintSnapshot()
    {
    }
}
