namespace Hpn.Modules.Appreciation.Internal.Domain;

internal sealed class GivenAppreciationStat
{
    public Guid SenderUserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public int Count { get; private set; }

    private GivenAppreciationStat()
    {
    }
}
