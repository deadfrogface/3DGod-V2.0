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
        services.AddSingleton<IFeatureAvailabilityService, DynamicFeatureAvailabilityService>();
        services.AddSingleton<AnnyHumanService>();
        services.AddSingleton<IProjectService, GodProjectArchive>();
        services.AddSingleton<AnnyPresetStore>();
        services.AddSingleton<IReferenceImageGenerationService, ReferenceImageService>();
        services.AddSingleton<IImageTo3DService, ImageTo3DService>();
        services.AddSingleton(sp =>
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod",
                "Recovery");
            return new AutosaveService(root);
        });
        services.AddSingleton<IWorkerHost, WorkerProcessHost>();
        services.AddSingleton<IBackendRegistry>(_ => new BackendRegistry(
        [
            new BackendManifest
            {
                Id = "echo",
                Priority = 1,
                State = BackendRuntimeState.Available,
                License = new LicenseProfile { Id = "mit", Accepted = true },
                Capabilities = new BackendCapabilities { HumanGenerate = true }
            },
            new BackendManifest
            {
                Id = "anny",
                Priority = 100,
                State = AnnyRuntime.Probe().Availability == FeatureAvailability.NotInstalled
                    ? BackendRuntimeState.NotInstalled
                    : BackendRuntimeState.Available,
                License = new LicenseProfile { Id = "apache-2.0", Accepted = true },
                Hardware = new HardwareRequirement { MinVramMb = 0, RequiresCuda = false },
                Capabilities = new BackendCapabilities { HumanGenerate = true }
            }
        ]));
        services.AddSingleton<IGpuJobScheduler, GpuJobScheduler>();
        services.AddSingleton<IDiagnosticService, DiagnosticService>();
        services.AddSingleton<CommandStack>();
        services.AddSingleton<CharacterSystem>();
        services.AddSingleton<ICharacterModelService, CharacterModelServiceAdapter>();
        return services;
    }
}
