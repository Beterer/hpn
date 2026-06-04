namespace Hpn.Modules.SocialFingerprint.Internal.Features.GetMyFingerprint;

internal static class FingerprintPhrasing
{
    public const string ReadyHeadline = "People often perceive you in these dimensions.";
    public const string ReadySummary = "This is a private, interpretive pattern from appreciations people have chosen for you.";
    public const string InsufficientHeadline = "Your fingerprint is still gathering enough perspective.";
    public const string InsufficientSummary = "It appears after at least 20 received appreciations, so it has enough different moments behind it.";

    public static string ForCategory(string slug, string label) => slug switch
    {
        "physical" => "People often perceive a warmth in how you come across.",
        "energy" => "People often perceive your energy.",
        "style" => "People often notice your sense of style.",
        "humor" => "People often perceive your humor.",
        "mind" => "People often perceive a thoughtful mind.",
        "authentic" => "People often perceive you as genuine.",
        _ => $"People often perceive {label.ToLowerInvariant()} in you.",
    };

    // Trait-level (ADR-025): a specific named trait, phrased as perception. Always
    // "perceive", never a count or rank.
    public static string ForTrait(string label) =>
        $"“{label}” is one of the recurring ways people perceive you.";
}
