namespace Hpn.Modules.Moderation.Internal.Domain;

/// <summary>Lifecycle of a single report (backbone §7.1 <c>moderation.report_status</c>).</summary>
internal enum ReportStatus
{
    Open,
    Reviewing,
    Actioned,
    Dismissed,
}
