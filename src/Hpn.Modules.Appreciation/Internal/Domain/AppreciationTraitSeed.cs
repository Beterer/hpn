namespace Hpn.Modules.Appreciation.Internal.Domain;

// The 20 seeded traits (ADR-025), grouped under the 6 categories. SortOrder is a
// single 1..20 sequence in category order so the flattened cloud reads as a soft
// rainbow. GUIDs are fixed (HasData seed) — do not renumber existing ones.
internal static class AppreciationTraitSeed
{
    private static Guid Id(int n) => new($"2a1d7f00-0000-4000-8000-{n:D12}");

    public static IReadOnlyList<AppreciationTrait> All { get; } =
    [
        // physical (hue 38)
        new(Id(1), AppreciationCategorySeed.Physical, "warm_smile", "Warm smile", 1),
        new(Id(2), AppreciationCategorySeed.Physical, "kind_eyes", "Kind eyes", 2),
        new(Id(3), AppreciationCategorySeed.Physical, "great_hair", "Great hair", 3),
        new(Id(4), AppreciationCategorySeed.Physical, "natural_glow", "Natural glow", 4),

        // energy (hue 78)
        new(Id(5), AppreciationCategorySeed.Energy, "good_vibe", "Good vibe", 5),
        new(Id(6), AppreciationCategorySeed.Energy, "confident", "Confident", 6),
        new(Id(7), AppreciationCategorySeed.Energy, "calm_presence", "Calm presence", 7),
        new(Id(8), AppreciationCategorySeed.Energy, "magnetic", "Magnetic", 8),

        // style (hue 350)
        new(Id(9), AppreciationCategorySeed.Style, "great_fit", "Great fit", 9),
        new(Id(10), AppreciationCategorySeed.Style, "effortless", "Effortless", 10),
        new(Id(11), AppreciationCategorySeed.Style, "signature_look", "Signature look", 11),

        // humor (hue 142)
        new(Id(12), AppreciationCategorySeed.Humor, "made_me_grin", "Made me grin", 12),
        new(Id(13), AppreciationCategorySeed.Humor, "quick_wit", "Quick wit", 13),
        new(Id(14), AppreciationCategorySeed.Humor, "wonderfully_odd", "Wonderfully odd", 14),

        // mind (hue 264)
        new(Id(15), AppreciationCategorySeed.Mind, "curious", "Curious", 15),
        new(Id(16), AppreciationCategorySeed.Mind, "thoughtful", "Thoughtful", 16),
        new(Id(17), AppreciationCategorySeed.Mind, "sharp", "Sharp", 17),

        // authentic (hue 200)
        new(Id(18), AppreciationCategorySeed.Authentic, "genuine", "Genuine", 18),
        new(Id(19), AppreciationCategorySeed.Authentic, "grounded", "Grounded", 19),
        new(Id(20), AppreciationCategorySeed.Authentic, "true_to_themselves", "True to themselves", 20),
    ];
}
