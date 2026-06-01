using Hpn.Modules.Identity.Contracts.Dtos;

namespace Hpn.Modules.Identity.Contracts;

/// <summary>
/// The only surface other modules may call into Identity through (backbone §6.1,
/// §3.3). Everything else in the module is <c>internal</c>.
/// </summary>
public interface IIdentityApi
{
    Task<IdentityUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default);
}
