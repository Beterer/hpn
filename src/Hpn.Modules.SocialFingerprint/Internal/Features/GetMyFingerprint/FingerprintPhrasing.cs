namespace Hpn.Modules.SocialFingerprint.Internal.Features.GetMyFingerprint;

internal static class FingerprintPhrasing
{
    public const string ReadyHeadline = "People often perceive you in these dimensions.";
    public const string ReadySummary = "This is a private, interpretive pattern from appreciations people have chosen for you.";
    public const string InsufficientHeadline = "Your fingerprint is still gathering enough perspective.";
    public const string InsufficientSummary = "It appears after at least 20 received appreciations, so it has enough different moments behind it.";

    public static string ForCategory(string slug, string label) => slug switch
    {
        "warm_smile" => "People often perceive warmth in your expression.",
        "authentic" => "People often perceive you as genuine.",
        "stylish" => "People often notice your sense of style.",
        "calming_energy" => "People often perceive a calming presence.",
        "confident" => "People often perceive quiet confidence.",
        "expressive" => "People often notice expressive energy.",
        "fun_energy" => "People often perceive a playful spark.",
        "elegant" => "People often notice an elegant quality.",
        "trustworthy" => "People often perceive trustworthiness.",
        "creative" => "People often perceive a creative spark.",
        "kind" => "People often perceive kindness.",
        "intelligent" => "People often perceive thoughtfulness.",
        _ => $"People often perceive {label.ToLowerInvariant()} in you.",
    };

    public static string ForTrait(string slug, string label) => slug switch
    {
        "warm_smile" => "Warmth is one of the recurring ways people perceive you.",
        "authentic" => "A genuine quality appears often in how people perceive you.",
        "stylish" => "Style appears often in how people notice you.",
        "calming_energy" => "A calming presence appears often in how people perceive you.",
        "confident" => "Quiet confidence appears often in how people perceive you.",
        "expressive" => "Expressiveness appears often in how people notice you.",
        "fun_energy" => "Fun energy appears often in how people perceive you.",
        "elegant" => "Elegance appears often in how people notice you.",
        "trustworthy" => "Trustworthiness appears often in how people perceive you.",
        "creative" => "Creativity appears often in how people perceive you.",
        "kind" => "Kindness appears often in how people perceive you.",
        "intelligent" => "Thoughtfulness appears often in how people perceive you.",
        _ => $"{label} appears often in how people perceive you.",
    };
}
