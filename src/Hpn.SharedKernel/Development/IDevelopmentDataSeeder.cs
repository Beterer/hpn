namespace Hpn.SharedKernel.Development;

public interface IDevelopmentDataSeeder
{
    int Phase { get; }

    Task SeedAsync(DevelopmentSeedContext context, CancellationToken cancellationToken = default);
}
