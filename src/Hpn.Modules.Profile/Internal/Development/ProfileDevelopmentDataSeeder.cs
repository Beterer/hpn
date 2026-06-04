using Hpn.Modules.Profile.Internal.Domain;
using Hpn.Modules.Profile.Internal.Persistence;
using Hpn.SharedKernel.Development;
using Microsoft.EntityFrameworkCore;

namespace Hpn.Modules.Profile.Internal.Development;

internal sealed class ProfileDevelopmentDataSeeder(
    ProfileDbContext dbContext,
    TimeProvider timeProvider) : IDevelopmentDataSeeder
{
    private static readonly string[] Names =
    [
        "Mira", "Rowan", "Iris", "Noor", "Theo", "Lina", "Sage", "Vera", "Eli", "Nadia",
        "Ari", "June", "Mika", "Rhea", "Sol", "Anya", "Kai", "Mara", "Nico", "Tess",
        "Owen", "Lea", "Ren", "Daria", "Milo", "Zara", "Cora", "Remy", "Alina", "Ivo",
    ];

    private static readonly string[] Countries = ["RO", "US", "DE", "FR", "NL", "ES", "IT", "GB"];

    public int Phase => 20;

    public async Task SeedAsync(DevelopmentSeedContext context, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var interests = await dbContext.Interests
            .OrderBy(i => i.Label)
            .ToArrayAsync(cancellationToken);

        await EnsureProfileAsync(
            "test",
            context.GetUser("test").UserId,
            "Test Notice",
            Gender.Woman,
            "RO",
            "A ready-to-use development profile with enough appreciation history to explore Notice.",
            verified: true,
            latitude: 44.4,
            longitude: 26.1,
            interests,
            interestOffset: 0,
            context,
            now,
            cancellationToken);

        var candidateCount = Math.Max(0, context.Options.CandidateCount);
        for (var i = 0; i < candidateCount; i++)
        {
            var key = context.CandidateKey(i);
            await EnsureProfileAsync(
                key,
                context.GetUser(key).UserId,
                Names[i % Names.Length],
                GenderFor(i),
                Countries[i % Countries.Length],
                BioFor(i),
                verified: i % 5 == 0,
                latitude: 44.4 + (i % 7 * 0.4),
                longitude: 26.1 + (i % 9 * 0.5),
                interests,
                interestOffset: i,
                context,
                now,
                cancellationToken);
        }
    }

    private async Task EnsureProfileAsync(
        string key,
        Guid userId,
        string displayName,
        Gender gender,
        string countryCode,
        string bio,
        bool verified,
        double latitude,
        double longitude,
        IReadOnlyList<Interest> interests,
        int interestOffset,
        DevelopmentSeedContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles
            .Include(p => p.ProfileInterests)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = UserProfile.Create(userId, displayName, gender, null, countryCode, bio, now);
            dbContext.Profiles.Add(profile);
        }
        else
        {
            profile.UpdateDetails(displayName, gender, null, countryCode, bio, now);
        }

        profile.SetVerified(verified, now);
        profile.SetLocation(latitude, longitude, consent: true, now);
        profile.VisibilityPreferences.Update(
            hideFromCountry: false,
            minDistanceKm: null,
            womenForWomen: false,
            verifiedOnly: false,
            paused: false,
            hiddenFromGuests: false);

        if (interests.Count > 0)
        {
            var selected = Enumerable.Range(0, Math.Min(3, interests.Count))
                .Select(i => interests[(interestOffset + i) % interests.Count])
                .ToArray();
            profile.ReplaceInterests(selected, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        context.SetProfile(key, profile.Id, userId);
    }

    private static Gender GenderFor(int index) => (index % 4) switch
    {
        0 => Gender.Woman,
        1 => Gender.Man,
        2 => Gender.NonBinary,
        _ => Gender.Woman,
    };

    private static string BioFor(int index) => (index % 6) switch
    {
        0 => "Usually found near good light, quiet music, and a half-finished notebook.",
        1 => "Likes generous details, fresh coffee, and people who notice small things.",
        2 => "Here for warm, specific appreciation and unhurried browsing.",
        3 => "Collects tiny rituals: long walks, film stills, late breakfasts.",
        4 => "Drawn to thoughtful style, bright conversation, and patient curiosity.",
        _ => "A development profile with enough texture to make the feed feel alive.",
    };
}

internal sealed class ProfileActivationDevelopmentDataSeeder(
    ProfileDbContext dbContext,
    TimeProvider timeProvider) : IDevelopmentDataSeeder
{
    public int Phase => 40;

    public async Task SeedAsync(DevelopmentSeedContext context, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var keys = new[] { "test" }
            .Concat(Enumerable.Range(0, Math.Max(0, context.Options.CandidateCount)).Select(context.CandidateKey))
            .ToArray();

        foreach (var key in keys)
        {
            var profileId = context.GetProfile(key).ProfileId;
            var profile = await dbContext.Profiles.FirstAsync(p => p.Id == profileId, cancellationToken);
            profile.Activate(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
