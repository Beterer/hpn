namespace Hpn.Modules.Appreciation.Internal.Domain;

internal sealed class AppreciationCategory
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Label { get; private set; } = null!;
    public int SortOrder { get; private set; }

    // OKLCH hue (degrees) for this category's accent (ADR-025). A category conveys
    // itself by colour alone in the flattened trait cloud; the hue is the single
    // value the client expands into dot/soft/ink shades.
    public int Hue { get; private set; }
    public bool Active { get; private set; }

    private AppreciationCategory()
    {
    }

    public AppreciationCategory(Guid id, string slug, string label, int sortOrder, int hue, bool active = true)
    {
        Id = id;
        Slug = slug;
        Label = label;
        SortOrder = sortOrder;
        Hue = hue;
        Active = active;
    }
}
