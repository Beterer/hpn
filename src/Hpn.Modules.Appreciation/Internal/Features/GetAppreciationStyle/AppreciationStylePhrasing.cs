namespace Hpn.Modules.Appreciation.Internal.Features.GetAppreciationStyle;

internal static class AppreciationStylePhrasing
{
    public const string Headline = "What you tend to notice";
    public const string EmptySummary = "As you appreciate people, a private pattern of your attention will gather here.";
    public const string Summary = "This is a private reading of the qualities you tend to notice in others.";

    public static string ForCategory(string label, int count, double difference)
    {
        var normalized = label.ToLowerInvariant();
        if (count == 0)
        {
            return $"{label} has not been a frequent part of what you notice yet.";
        }

        if (difference >= 0.1)
        {
            return $"You tend to notice {normalized} more often than the wider Notice pattern.";
        }

        if (difference <= -0.1)
        {
            return $"You notice {normalized} more lightly than the wider Notice pattern.";
        }

        return $"Your attention to {normalized} sits close to the wider Notice pattern.";
    }
}
