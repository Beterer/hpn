namespace Hpn.Modules.Profile.Contracts;

/// <summary>
/// Extension point for modules that contribute activation requirements without
/// making Profile depend on their internals or creating a project cycle.
/// </summary>
public interface IProfileActivationRequirement
{
    Task<ProfileActivationRequirementResult> CheckAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);
}

public sealed record ProfileActivationRequirementResult(
    bool Satisfied,
    string ProblemType,
    string Title,
    string Detail)
{
    public static ProfileActivationRequirementResult Pass { get; } = new(
        Satisfied: true,
        ProblemType: string.Empty,
        Title: string.Empty,
        Detail: string.Empty);
}
