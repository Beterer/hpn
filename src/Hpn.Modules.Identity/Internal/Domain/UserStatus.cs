namespace Hpn.Modules.Identity.Internal.Domain;

/// <summary>Account lifecycle at the identity level (backbone §7.2).</summary>
internal enum UserStatus
{
    Active,
    Deactivated,

    /// <summary>Deletion requested; awaiting the grace-window hard purge (§10.5).</summary>
    PendingDeletion,
}
