namespace Hpn.Modules.Moderation.Internal.Domain;

/// <summary>The reason a profile was reported (backbone §7.1 <c>moderation.report_type</c>).</summary>
internal enum ReportType
{
    AiGenerated,
    FakeProfile,
    InappropriateContent,
    StolenPhotos,
    Spam,
    Nsfw,
    Harassment,
    Underage,
}
