using Hpn.SharedKernel.Modules;

namespace Hpn.Api.Infrastructure;

internal static class ModuleInitializationExtensions
{
    /// <summary>
    /// Runs every module's <see cref="IModuleInitializer"/> on startup (applies
    /// migrations, seeds reference data). In Development this happens on boot; in
    /// prod migrations are a gated deploy step, not auto-applied (backbone §13.3).
    /// </summary>
    public static async Task InitializeModulesAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        foreach (var initializer in scope.ServiceProvider.GetServices<IModuleInitializer>())
        {
            await initializer.InitializeAsync();
        }
    }
}
