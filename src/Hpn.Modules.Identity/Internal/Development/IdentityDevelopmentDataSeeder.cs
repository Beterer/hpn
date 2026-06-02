using Hpn.Modules.Identity.Internal.Domain;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.SharedKernel.Development;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Identity.Internal.Development;

internal sealed class IdentityDevelopmentDataSeeder(
    IdentityDbContext dbContext,
    TimeProvider timeProvider) : IDevelopmentDataSeeder
{
    public int Phase => 10;

    public async Task SeedAsync(DevelopmentSeedContext context, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await EnsureUserAsync("test", context.Options.TestEmail, context, now, cancellationToken);

        var candidateCount = Math.Max(0, context.Options.CandidateCount);
        for (var i = 0; i < candidateCount; i++)
        {
            await EnsureUserAsync(
                context.CandidateKey(i),
                $"seed-candidate-{i + 1:D2}@notice.local",
                context,
                now,
                cancellationToken);
        }

        var observerCount = Math.Max(0, context.Options.IncomingAppreciationCount);
        for (var i = 0; i < observerCount; i++)
        {
            await EnsureUserAsync(
                context.ObserverKey(i),
                $"seed-observer-{i + 1:D2}@notice.local",
                context,
                now,
                cancellationToken);
        }
    }

    private async Task EnsureUserAsync(
        string key,
        string email,
        DevelopmentSeedContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            user = User.Register(normalizedEmail, now);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        context.SetUser(key, user.Id, user.Email);
    }
}
