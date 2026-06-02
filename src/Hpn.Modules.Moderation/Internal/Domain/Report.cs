namespace Hpn.Modules.Moderation.Internal.Domain;

/// <summary>
/// One report of a profile by a member (backbone §7.8). Reports are intake only —
/// they never act on their own; weighted pressure across distinct reporters drives
/// any automatic restriction (§10.3). A reporter may report a given target for a
/// given type at most once (enforced by a unique index); re-reports collapse.
/// </summary>
internal sealed class Report
{
    public Guid Id { get; private set; }
    public Guid ReporterUserId { get; private set; }
    public Guid TargetProfileId { get; private set; }
    public ReportType Type { get; private set; }
    public string? Note { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private Report()
    {
    }

    public static Report Create(
        Guid reporterUserId,
        Guid targetProfileId,
        ReportType type,
        string? note,
        DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        ReporterUserId = reporterUserId,
        TargetProfileId = targetProfileId,
        Type = type,
        Note = Normalize(note),
        Status = ReportStatus.Open,
        CreatedAt = now,
    };

    /// <summary>Moves the report into the review queue when its target is restricted.</summary>
    public void MarkReviewing()
    {
        if (Status == ReportStatus.Open)
        {
            Status = ReportStatus.Reviewing;
        }
    }

    /// <summary>Resolves the report as upheld (the target was actioned).</summary>
    public void MarkActioned(DateTimeOffset now)
    {
        Status = ReportStatus.Actioned;
        ResolvedAt = now;
    }

    /// <summary>Resolves the report as dismissed (no action warranted).</summary>
    public void MarkDismissed(DateTimeOffset now)
    {
        Status = ReportStatus.Dismissed;
        ResolvedAt = now;
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
