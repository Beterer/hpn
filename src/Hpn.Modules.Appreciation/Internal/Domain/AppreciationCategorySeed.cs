namespace Hpn.Modules.Appreciation.Internal.Domain;

// Six categories, each carrying its OKLCH hue (ADR-025). Hues are spaced so the
// flattened trait cloud reads like a soft rainbow when traits are listed in
// category order. The specific traits live in AppreciationTraitSeed.
internal static class AppreciationCategorySeed
{
    public static readonly Guid Physical = new("0f93fb39-2e34-4c90-bf9d-28df31447301");
    public static readonly Guid Energy = new("0f93fb39-2e34-4c90-bf9d-28df31447302");
    public static readonly Guid Style = new("0f93fb39-2e34-4c90-bf9d-28df31447303");
    public static readonly Guid Humor = new("0f93fb39-2e34-4c90-bf9d-28df31447304");
    public static readonly Guid Mind = new("0f93fb39-2e34-4c90-bf9d-28df31447305");
    public static readonly Guid Authentic = new("0f93fb39-2e34-4c90-bf9d-28df31447306");

    public static IReadOnlyList<AppreciationCategory> All { get; } =
    [
        new(Physical, "physical", "Physical", 1, 38),
        new(Energy, "energy", "Energy", 2, 78),
        new(Style, "style", "Style", 3, 350),
        new(Humor, "humor", "Humor", 4, 142),
        new(Mind, "mind", "Mind", 5, 264),
        new(Authentic, "authentic", "Authentic", 6, 200),
    ];
}
