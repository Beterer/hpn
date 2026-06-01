namespace Hpn.Modules.Appreciation.Internal.Domain;

internal static class AppreciationCategorySeed
{
    public static IReadOnlyList<AppreciationCategory> All { get; } =
    [
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447201"), "warm_smile", "Warm smile", 1),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447202"), "authentic", "Authentic", 2),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447203"), "stylish", "Stylish", 3),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447204"), "calming_energy", "Calming energy", 4),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447205"), "confident", "Confident", 5),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447206"), "expressive", "Expressive", 6),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447207"), "fun_energy", "Fun energy", 7),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447208"), "elegant", "Elegant", 8),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447209"), "trustworthy", "Trustworthy", 9),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447210"), "creative", "Creative", 10),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447211"), "kind", "Kind", 11),
        new(new Guid("0f93fb39-2e34-4c90-bf9d-28df31447212"), "intelligent", "Intelligent-looking", 12),
    ];
}
