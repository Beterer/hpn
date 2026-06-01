namespace Hpn.SharedKernel.Modules;

/// <summary>
/// A module's startup hook. The host resolves every registered initializer in a
/// scope on boot and runs it — the sanctioned place to apply that module's
/// migrations and seed reference data (backbone §13.3). The host never references
/// a module's internal DbContext directly; this is the seam instead.
/// </summary>
public interface IModuleInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
