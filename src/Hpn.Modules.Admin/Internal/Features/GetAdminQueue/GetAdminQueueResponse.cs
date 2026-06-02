namespace Hpn.Modules.Admin.Internal.Features.GetAdminQueue;

internal sealed record AdminQueueItemResponse(
    Guid ProfileId,
    Guid TargetUserId,
    string DisplayName,
    string ProfileStatus,
    int ReportCount,
    DateTimeOffset LatestReportAt,
    double TrustScore);
