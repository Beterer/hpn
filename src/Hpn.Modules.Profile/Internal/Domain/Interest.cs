namespace Hpn.Modules.Profile.Internal.Domain;

internal sealed class Interest
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Label { get; private set; } = null!;

    private Interest()
    {
    }

    public Interest(Guid id, string slug, string label)
    {
        Id = id;
        Slug = slug;
        Label = label;
    }
}
