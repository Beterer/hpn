namespace Hpn.Modules.Moderation.Internal.Features.SubmitReport;

/// <summary>
/// Reports a profile (backbone §8 Reports). <c>Type</c> is one of the
/// <c>moderation.report_type</c> values (§7.1); <c>Note</c> is an optional free-text
/// detail. Low-friction by design, but rate-limited and de-duplicated per
/// reporter/target/type (§10.3).
/// </summary>
internal sealed record SubmitReportRequest(Guid TargetProfileId, string Type, string? Note);

/// <summary>Acknowledges intake. No counts or outcomes are exposed to the reporter
/// (§2): a report is received, never "scored".</summary>
internal sealed record SubmitReportResponse(Guid ReportId, string Status);
