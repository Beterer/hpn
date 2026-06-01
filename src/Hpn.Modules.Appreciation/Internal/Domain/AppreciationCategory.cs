namespace Hpn.Modules.Appreciation.Internal.Domain;

internal sealed class AppreciationCategory
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Label { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool Active { get; private set; }

    private AppreciationCategory()
    {
    }

    public AppreciationCategory(Guid id, string slug, string label, int sortOrder, bool active = true)
    {
        Id = id;
        Slug = slug;
        Label = label;
        SortOrder = sortOrder;
        Active = active;
    }
}
