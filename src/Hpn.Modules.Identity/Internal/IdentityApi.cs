using Hpn.Modules.Identity.Contracts;
using Hpn.Modules.Identity.Contracts.Dtos;
using Hpn.Modules.Identity.Internal.Domain;
using Hpn.Modules.Identity.Internal.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Identity.Internal;

/// <summary>
/// Read-only implementation of the cross-module contract over the identity
/// schema. No writes here — those stay in the module's command handlers (§3.3).
/// </summary>
internal sealed class IdentityApi(IdentityDbContext dbContext) : IIdentityApi
{
    public async Task<IdentityUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new IdentityUserDto(u.Id, u.Email, u.Role.ToString().ToLowerInvariant(), u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);

    public Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(u => u.Id == userId && u.Role == UserRole.Admin, cancellationToken);
}
