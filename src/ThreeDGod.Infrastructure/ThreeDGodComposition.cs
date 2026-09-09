using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Editing;
using ThreeDGod.Workers;
using ThreeDGod.Persistence;
using ThreeDGod.Physics;
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
        services.AddSingleton<IDiagnosticService, DiagnosticService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton<PresetService>();
        services.AddSingleton<IBlenderOperations, LegacyBlenderBackend>();
        services.AddSingleton<IFeatureAvailabilityService, DynamicFeatureAvailabilityService>();
        services.AddSingleton<IWorkerHost>(sp => new WorkerProcessHost(sp.GetRequiredService<IDiagnosticService>()));
        services.AddSingleton<AnnyHumanService>();
        services.AddSingleton<GarmentCodeService>();
        services.AddSingleton<IGarmentCodeService>(sp => sp.GetRequiredService<GarmentCodeService>());
        services.AddSingleton<IProjectService>(sp => new GodProjectArchive(sp.GetRequiredService<IDiagnosticService>()));
        services.AddSingleton<AnnyPresetStore>();
        services.AddSingleton<IReferenceImageGenerationService, ReferenceImageService>();
        services.AddSingleton<IImageTo3DService, ImageTo3DService>();
        services.AddSingleton<AssetLibrary>();
        services.AddSingleton<IAssetGenerationService, AiAssetPipeline>();
        services.AddSingleton<IRemeshService, RemeshService>();
        services.AddSingleton<ISkinTokensRigService, SkinTokensRigService>();
        services.AddSingleton<IRigValidator, RigValidationService>();
        services.AddSingleton<ICreatureAssembly, CreatureAssembly>();
        services.AddSingleton<ICreatureTextEditService, CreatureTextEditService>();
        services.AddSingleton<IFreeformCharacterPipeline, FreeformPipeline>();
        services.AddSingleton<IGarmentService, GarmentService>();
        services.AddSingleton<IGarmentFitService, GarmentFitService>();
        services.AddSingleton<IGarmentSkinService, GarmentSkinService>();
        services.AddSingleton<IAccessoryPhysicsService, AccessoryPhysicsService>();
        services.AddSingleton<ThreeDGod.Rendering.ViewportSelectionService>();
        services.AddSingleton(sp =>
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod",
                "Recovery");
            return new AutosaveService(root);
        });
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
            },
            ImageTo3DManifest("triposr", 80, "mit", accepted: true, minVram: 0, cuda: false),
            ImageTo3DManifest("sf3d", 70, StabilityLicense.ProfileId, StabilityLicense.IsAccepted("sf3d"), 8192, cuda: true),
            ImageTo3DManifest("spar3d", 60, StabilityLicense.ProfileId, StabilityLicense.IsAccepted("spar3d"), 6144, cuda: true),
            ImageTo3DManifest("trellis", 20, "trellis", accepted: false, minVram: 12288, cuda: true)
        ]));
        services.AddSingleton<IGpuJobScheduler, GpuJobScheduler>();
        services.AddSingleton<CommandStack>();
        services.AddSingleton<CharacterSystem>();
        services.AddSingleton<ICharacterModelService, CharacterModelServiceAdapter>();
        return services;
    }

    private static BackendManifest ImageTo3DManifest(string id, int priority, string licenseId, bool accepted, int minVram, bool cuda)
    {
        var probe = ImageTo3DRuntime.Probe(id);
        var state = probe.Availability switch
        {
            FeatureAvailability.Experimental or FeatureAvailability.Available => BackendRuntimeState.Available,
            FeatureAvailability.UnsupportedHardware => BackendRuntimeState.UnsupportedHardware,
            FeatureAvailability.Disabled => BackendRuntimeState.LicenseBlocked,
            _ => BackendRuntimeState.NotInstalled
        };
        return new BackendManifest
        {
            Id = id,
            Priority = priority,
            State = state,
            License = new LicenseProfile { Id = licenseId, Accepted = accepted },
            Hardware = new HardwareRequirement { MinVramMb = minVram, RequiresCuda = cuda },
            Capabilities = new BackendCapabilities { ImageTo3d = true }
        };
    }
}
