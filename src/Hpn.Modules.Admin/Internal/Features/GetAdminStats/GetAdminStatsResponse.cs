namespace Hpn.Modules.Admin.Internal.Features.GetAdminStats;

internal sealed record AdminStatsResponse(
    int OpenReports,
    int ReviewingReports,
    int ActionedReports,
    int DismissedReports,
    int CurrentlyRestricted,
    int CurrentlyBanned,
    int VerifiedProfiles,
    double AverageTrustScore);
