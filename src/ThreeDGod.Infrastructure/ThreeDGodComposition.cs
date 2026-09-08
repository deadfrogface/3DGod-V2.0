using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Editing;
using ThreeDGod.Workers;
using ThreeDGod.Persistence;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGod.Infrastructure;

public static class ThreeDGodComposition
{
    /// <summary>
    /// Registers only services that have a real implementation.
    /// Unimplemented capability interfaces are intentionally omitted.
    /// </summary>
    public static IServiceCollection AddThreeDGodCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<ConfigService>();
        services.AddSingleton<PresetService>();
        services.AddSingleton<IBlenderOperations, LegacyBlenderBackend>();
        services.AddSingleton<IFeatureAvailabilityService, FeatureAvailabilityService>();
        services.AddSingleton<IProjectService, GodProjectArchive>();
        services.AddSingleton(sp =>
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod",
                "Recovery");
            return new AutosaveService(root);
        });
        services.AddSingleton<IWorkerHost, WorkerProcessHost>();
        services.AddSingleton<IDiagnosticService, DiagnosticService>();
        services.AddSingleton<CommandStack>();
        services.AddSingleton<CharacterSystem>();
        services.AddSingleton<ICharacterModelService, CharacterModelServiceAdapter>();
        return services;
    }
}
