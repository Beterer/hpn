namespace Hpn.Modules.Admin.Internal.ReadModel;

/// <summary>
/// Sanctioned Admin read models over moderation/profile/identity schemas (§3.1,
/// §6.8). They are query-only and are never migrated as Admin-owned tables.
/// </summary>
internal sealed class AdminQueueItemReadModel
{
    public Guid ProfileId { get; set; }
    public Guid TargetUserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ProfileStatus { get; set; } = string.Empty;
    public int ReportCount { get; set; }
    public DateTimeOffset LatestReportAt { get; set; }
    public double TrustScore { get; set; }
}

internal sealed class AdminReportReadModel
{
    public Guid ReportId { get; set; }
    public Guid ReporterUserId { get; set; }
    public Guid TargetProfileId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

internal sealed class AdminStatsReadModel
{
    public int OpenReports { get; set; }
    public int ReviewingReports { get; set; }
    public int ActionedReports { get; set; }
    public int DismissedReports { get; set; }
    public int CurrentlyRestricted { get; set; }
    public int CurrentlyBanned { get; set; }
    public int VerifiedProfiles { get; set; }
    public double AverageTrustScore { get; set; }
}
