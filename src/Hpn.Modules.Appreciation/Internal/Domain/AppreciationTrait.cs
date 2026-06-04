namespace Hpn.Modules.Appreciation.Internal.Domain;

// A specific, named appreciation under a category (ADR-025). The picker shows all
// active traits flattened into one colour-coded cloud; the trait's category gives
// it its hue. SortOrder is global (category order) so the cloud clusters by colour.
internal sealed class AppreciationTrait
{
    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Label { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool Active { get; private set; }

    private AppreciationTrait()
    {
    }

    public AppreciationTrait(Guid id, Guid categoryId, string slug, string label, int sortOrder, bool active = true)
    {
        Id = id;
        CategoryId = categoryId;
        Slug = slug;
        Label = label;
        SortOrder = sortOrder;
        Active = active;
    }
}
