using Hpn.Modules.Identity.Contracts.Events;
using Hpn.Modules.Identity.Internal.Domain;
using Hpn.Modules.Identity.Internal.Email;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.Modules.Identity.Internal.Security;
using Hpn.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Hpn.Modules.Identity.Internal.Features.RequestMagicLink;

/// <summary>
/// Find-or-create the account, issue a single-use hashed token, and email the
/// link (backbone §10.1). The endpoint always answers 202 regardless of outcome,
/// so nothing here may reveal whether an account existed (no enumeration, §8.1).
/// Per-email volume is capped in addition to the per-IP rate limit (§10.6).
/// </summary>
internal sealed class RequestMagicLinkHandler(
    IdentityDbContext dbContext,
    IEmailSender emailSender,
    IDomainEventDispatcher eventDispatcher,
    TimeProvider timeProvider,
    IOptions<IdentityOptions> options,
    ILogger<RequestMagicLinkHandler> logger)
{
    private readonly IdentityOptions _options = options.Value;

    public async Task HandleAsync(RequestMagicLinkRequest request, string? requestIp, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow();

        var user = await ResolveAccountAsync(email, now, cancellationToken);

        // Per-email throttle: cap live (unexpired) tokens issued for this account.
        var liveTokens = await dbContext.MagicLinkTokens
            .CountAsync(t => t.UserId == user.Id && t.ConsumedAt == null && t.ExpiresAt > now, cancellationToken);
        if (liveTokens >= _options.MaxMagicLinksPerEmailPerWindow)
        {
            logger.LogInformation("Magic-link request throttled for an account (per-email cap reached).");
            return;
        }

        var rawToken = SecureTokenGenerator.Generate();
        dbContext.MagicLinkTokens.Add(
            MagicLinkToken.Issue(user.Id, TokenHasher.Hash(rawToken), now, _options.MagicLinkLifetime, requestIp));
        await dbContext.SaveChangesAsync(cancellationToken);

        await emailSender.SendMagicLinkAsync(email, BuildMagicLinkUrl(rawToken), cancellationToken);
    }

    private async Task<User> ResolveAccountAsync(string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        // New account: create it and raise UserRegistered inside one transaction
        // so a same-context subscriber commits atomically with the user (§10.7).
        // Cross-module atomicity is a deferred concern (no outbox in v1, §12).
        var user = User.Register(email, now);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            await eventDispatcher.DispatchAsync(new UserRegistered(user.Id, user.Email), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException ex) when (IsEmailConflict(ex))
        {
            // A concurrent first request already created this account; adopt it so
            // the endpoint still answers 202 without enumerating (§10.1).
            await transaction.RollbackAsync(cancellationToken);
            dbContext.Entry(user).State = EntityState.Detached;
            return await dbContext.Users.FirstAsync(u => u.Email == email, cancellationToken);
        }
    }

    private static bool IsEmailConflict(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private string BuildMagicLinkUrl(string rawToken)
    {
        var baseUrl = _options.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/auth/verify?token={Uri.EscapeDataString(rawToken)}";
    }
}
