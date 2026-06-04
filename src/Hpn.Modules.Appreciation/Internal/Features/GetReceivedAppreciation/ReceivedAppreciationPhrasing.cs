namespace Hpn.Modules.Appreciation.Internal.Features.GetReceivedAppreciation;

// Perception phrasing keyed by trait slug (ADR-025). Always "People often…" /
// "Someone…", never a count or a score (product principle: perception, not ranking).
internal static class ReceivedAppreciationPhrasing
{
    public const string Headline = "Quietly, people keep noticing you.";
    public const string EmptySummary = "As people notice you, their appreciations will gather here privately.";
    public const string Summary =
        "These are the words others chose, kept private and shown only to you. No counts are ever made public.";

    public static string ForTrait(string slug, string label) => slug switch
    {
        "warm_smile" => "People often notice your warm smile.",
        "kind_eyes" => "People often notice the kindness in your eyes.",
        "great_hair" => "People often notice your great hair.",
        "natural_glow" => "People often notice your natural glow.",
        "good_vibe" => "Your energy reads as easy and open.",
        "confident" => "People often pick up on your quiet confidence.",
        "calm_presence" => "Being around you tends to feel calm.",
        "magnetic" => "People often find you magnetic.",
        "great_fit" => "People often notice how well your look comes together.",
        "effortless" => "Your way of putting things together looks easy.",
        "signature_look" => "People often notice your signature look.",
        "made_me_grin" => "You often make people grin.",
        "quick_wit" => "People often enjoy your quick wit.",
        "wonderfully_odd" => "People often delight in how wonderfully odd you are.",
        "curious" => "People often notice how curious you are.",
        "thoughtful" => "People notice the care behind what you say.",
        "sharp" => "People often notice how sharp you are.",
        "genuine" => "People experience you as real and unguarded.",
        "grounded" => "People often feel how grounded you are.",
        "true_to_themselves" => "People often notice how true to yourself you are.",
        _ => $"People often describe you as {label.ToLowerInvariant()}.",
    };

    public static string ForEvent(string slug, string label) => slug switch
    {
        "warm_smile" => "Someone noticed your warm smile.",
        "kind_eyes" => "Someone noticed the kindness in your eyes.",
        "great_hair" => "Someone noticed your great hair.",
        "natural_glow" => "Someone noticed your natural glow.",
        "good_vibe" => "Someone felt your good vibe.",
        "confident" => "Someone noticed your confidence.",
        "calm_presence" => "Someone felt calm around you.",
        "magnetic" => "Someone found you magnetic.",
        "great_fit" => "Someone noticed your great fit.",
        "effortless" => "Someone noticed how effortless you are.",
        "signature_look" => "Someone noticed your signature look.",
        "made_me_grin" => "You made someone grin.",
        "quick_wit" => "Someone enjoyed your quick wit.",
        "wonderfully_odd" => "Someone found you wonderfully odd.",
        "curious" => "Someone noticed how curious you are.",
        "thoughtful" => "Someone found you thoughtful.",
        "sharp" => "Someone noticed how sharp you are.",
        "genuine" => "Someone found you genuine.",
        "grounded" => "Someone felt how grounded you are.",
        "true_to_themselves" => "Someone noticed how true to yourself you are.",
        _ => $"Someone described you as {label.ToLowerInvariant()}.",
    };
}
