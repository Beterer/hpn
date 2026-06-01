namespace Hpn.Modules.Appreciation.Internal.Domain;

internal sealed class ReceivedAppreciationStat
{
    public Guid ReceiverProfileId { get; private set; }
    public Guid CategoryId { get; private set; }
    public int Count { get; private set; }
    public DateTimeOffset LastAt { get; private set; }

    private ReceivedAppreciationStat()
    {
    }
}
