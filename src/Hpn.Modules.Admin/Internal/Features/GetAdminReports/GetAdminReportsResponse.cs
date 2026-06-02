namespace Hpn.Modules.Admin.Internal.Features.GetAdminReports;

internal sealed record AdminReportResponse(
    Guid ReportId,
    Guid ReporterUserId,
    Guid TargetProfileId,
    Guid? TargetUserId,
    string? TargetDisplayName,
    string Type,
    string Status,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);
