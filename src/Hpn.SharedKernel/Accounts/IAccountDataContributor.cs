namespace Hpn.SharedKernel.Accounts;

/// <summary>
/// A module's slice of an account's data, for GDPR export and hard deletion
/// (backbone §10.5). Each module implements one contributor over <em>its own</em>
/// schema only — the cross-module orchestrator never reaches into another
/// module's tables, it just fans out to these (write isolation stays intact,
/// §3.3). Implementations must be idempotent: erasing an already-erased account
/// is a no-op, so a re-run after a partial failure is safe.
/// </summary>
public interface IAccountDataContributor
{
    /// <summary>Stable key this contributor's data appears under in the export bundle.</summary>
    string Section { get; }

    /// <summary>
    /// True for the contributor that owns the canonical account (the user row every
    /// other slice references by id). The orchestrator erases it last. Default false;
    /// exactly one contributor should set it true.
    /// </summary>
    bool IsAccountRoot => false;

    /// <summary>The module's view of the account, or null when it holds nothing for it.</summary>
    Task<object?> ExportAsync(AccountScope scope, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes the module's data for the account, including any blobs it owns.</summary>
    Task EraseAsync(AccountScope scope, CancellationToken cancellationToken = default);
}
