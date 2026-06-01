namespace Hpn.Modules.Appreciation.Internal.Features.GetReceivedAppreciation;

internal static class ReceivedAppreciationPhrasing
{
    public const string Headline = "People often describe you in these ways.";
    public const string EmptySummary = "As people notice you, their appreciations will gather here privately.";
    public const string Summary = "These are private reflections of how people have perceived you so far.";

    public static string ForCategory(string slug, string label) => slug switch
    {
        "warm_smile" => "People often notice your warm smile.",
        "authentic" => "People often experience you as genuine and real.",
        "stylish" => "People often notice your sense of style.",
        "calming_energy" => "People often describe your presence as calming.",
        "confident" => "People often pick up on your quiet confidence.",
        "expressive" => "People often notice how expressive you are.",
        "fun_energy" => "People often describe your energy as fun.",
        "elegant" => "People often notice a certain elegance about you.",
        "trustworthy" => "People often sense that you're someone to trust.",
        "creative" => "People often perceive a creative spark in you.",
        "kind" => "People often feel your kindness.",
        "intelligent" => "People often perceive a thoughtful intelligence in you.",
        _ => $"People often describe you as {label.ToLowerInvariant()}.",
    };

    public static string ForEvent(string slug, string label) => slug switch
    {
        "warm_smile" => "Someone noticed your warm smile.",
        "authentic" => "Someone experienced you as genuine.",
        "stylish" => "Someone noticed your sense of style.",
        "calming_energy" => "Someone described your presence as calming.",
        "confident" => "Someone noticed your quiet confidence.",
        "expressive" => "Someone noticed how expressive you are.",
        "fun_energy" => "Someone described your energy as fun.",
        "elegant" => "Someone noticed a certain elegance about you.",
        "trustworthy" => "Someone sensed you're someone to trust.",
        "creative" => "Someone noticed your creative spark.",
        "kind" => "Someone felt your kindness.",
        "intelligent" => "Someone perceived a thoughtful intelligence in you.",
        _ => $"Someone described you as {label.ToLowerInvariant()}.",
    };
}
