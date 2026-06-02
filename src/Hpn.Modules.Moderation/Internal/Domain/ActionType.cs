namespace Hpn.Modules.Moderation.Internal.Domain;

/// <summary>A moderation decision recorded against an account (backbone §7.1
/// <c>moderation.action_type</c>). <see cref="TempRestrict"/> is the only one the
/// system applies automatically; the rest are admin/system decisions (§10.3).</summary>
internal enum ActionType
{
    Warn,
    TempRestrict,
    Ban,
    Clear,
}
