using Hpn.Modules.Profile.Contracts;
using Hpn.SharedKernel.Accounts;

namespace Hpn.Modules.Identity.Internal.Accounts;

/// <summary>
/// Fans an account-wide export or erasure out to every module's
/// <see cref="IAccountDataContributor"/> (backbone §10.5). It owns no other
/// module's data — it only resolves the <see cref="AccountScope"/> once (so a
/// contributor never has to, and erase can't race the profile row's own deletion)
/// and invokes the contract. Write isolation is preserved: each contributor still
/// only touches its own schema (§3.3).
/// </summary>
internal sealed class AccountDataOrchestrator(
    IEnumerable<IAccountDataContributor> contributors,
    IProfileApi profileApi)
{
    public async Task<IReadOnlyDictionary<string, object?>> ExportAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var scope = await BuildScopeAsync(userId, cancellationToken);
        var bundle = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var contributor in contributors.OrderBy(c => c.Section, StringComparer.Ordinal))
        {
            bundle[contributor.Section] = await contributor.ExportAsync(scope, cancellationToken);
        }

        return bundle;
    }

    public async Task EraseAsync(AccountScope scope, CancellationToken cancellationToken)
    {
        // The caller supplies a scope with the profile id resolved durably (from the
        // user row), so erasure is retry-safe even after the profile row is gone. The
        // account root (the user row everything references) is erased last.
        var ordered = contributors
            .OrderBy(c => c.IsAccountRoot ? 1 : 0)
            .ThenBy(c => c.Section, StringComparer.Ordinal);

        foreach (var contributor in ordered)
        {
            await contributor.EraseAsync(scope, cancellationToken);
        }
    }

    private async Task<AccountScope> BuildScopeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profileId = await profileApi.GetProfileIdForUserAsync(userId, cancellationToken);
        return new AccountScope(userId, profileId);
    }
}
