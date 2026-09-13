using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class CompositionTests
{
    [Fact]
    public void Di_ResolvesRealCoreServices_AndOmitsUnimplementedCapabilities()
    {
        var services = new ServiceCollection();
        services.AddThreeDGodCoreServices();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ConfigService>());
        Assert.NotNull(provider.GetRequiredService<PresetService>());
        Assert.NotNull(provider.GetRequiredService<IBlenderOperations>());
        Assert.IsType<LegacyBlenderBackend>(provider.GetRequiredService<IBlenderOperations>());
        Assert.NotNull(provider.GetRequiredService<CharacterSystem>());
        Assert.NotNull(provider.GetRequiredService<ICharacterModelService>());
        Assert.NotNull(provider.GetRequiredService<IFeatureAvailabilityService>());
        Assert.IsType<DynamicFeatureAvailabilityService>(provider.GetRequiredService<IFeatureAvailabilityService>());

        Assert.NotNull(provider.GetRequiredService<IProjectService>());
        Assert.IsType<GodProjectArchive>(provider.GetRequiredService<IProjectService>());
        Assert.NotNull(provider.GetRequiredService<IWorkerHost>());
        Assert.IsType<ThreeDGod.Workers.WorkerProcessHost>(provider.GetRequiredService<IWorkerHost>());
        Assert.NotNull(provider.GetRequiredService<ThreeDGod.Workers.AnnyHumanService>());
        Assert.NotNull(provider.GetRequiredService<IGarmentCodeService>());
        Assert.IsType<ThreeDGod.Workers.GarmentCodeService>(provider.GetRequiredService<IGarmentCodeService>());
        Assert.NotNull(provider.GetRequiredService<AnnyPresetStore>());
        Assert.NotNull(provider.GetService<IBackendRegistry>());
        Assert.Equal(FeatureAvailability.NotInstalled, provider.GetRequiredService<IImportService>().Probe());
        Assert.Equal(FeatureAvailability.Available, provider.GetRequiredService<IExportService>().Probe());
        Assert.Equal(FeatureAvailability.NotInstalled, provider.GetRequiredService<IRiggingService>().Probe());
        Assert.Equal(FeatureAvailability.NotInstalled, provider.GetRequiredService<IAutoRigBackend>().Probe());
        Assert.NotNull(provider.GetService<IMeshProcessor>());
        Assert.NotNull(provider.GetService<IImageTo3DService>());
        Assert.IsType<ImageTo3DService>(provider.GetRequiredService<IImageTo3DService>());
        Assert.NotNull(provider.GetRequiredService<IAssetGenerationService>());
        Assert.IsType<AiAssetPipeline>(provider.GetRequiredService<IAssetGenerationService>());
        Assert.NotNull(provider.GetRequiredService<IRemeshService>());
        Assert.IsType<RemeshService>(provider.GetRequiredService<IRemeshService>());
        Assert.NotNull(provider.GetRequiredService<ISkinTokensRigService>());
        Assert.IsType<SkinTokensRigService>(provider.GetRequiredService<ISkinTokensRigService>());
        Assert.NotNull(provider.GetRequiredService<IRigValidator>());
        Assert.IsType<RigValidationService>(provider.GetRequiredService<IRigValidator>());
        Assert.NotNull(provider.GetRequiredService<ICreatureAssembly>());
        Assert.IsType<CreatureAssembly>(provider.GetRequiredService<ICreatureAssembly>());
        Assert.NotNull(provider.GetRequiredService<ICreatureTextEditService>());
        Assert.IsType<CreatureTextEditService>(provider.GetRequiredService<ICreatureTextEditService>());
        Assert.NotNull(provider.GetRequiredService<IFreeformCharacterPipeline>());
        Assert.IsType<FreeformPipeline>(provider.GetRequiredService<IFreeformCharacterPipeline>());
        Assert.NotNull(provider.GetRequiredService<IGarmentService>());
        Assert.IsType<GarmentService>(provider.GetRequiredService<IGarmentService>());
        Assert.NotNull(provider.GetRequiredService<IGarmentFitService>());
        Assert.IsType<GarmentFitService>(provider.GetRequiredService<IGarmentFitService>());
        Assert.NotNull(provider.GetRequiredService<IGarmentSkinService>());
        Assert.IsType<GarmentSkinService>(provider.GetRequiredService<IGarmentSkinService>());
        Assert.NotNull(provider.GetRequiredService<IAccessoryPhysicsService>());
        Assert.IsType<ThreeDGod.Physics.AccessoryPhysicsService>(provider.GetRequiredService<IAccessoryPhysicsService>());
        Assert.NotNull(provider.GetRequiredService<IReferenceImageGenerationService>());
        Assert.IsType<ReferenceImageService>(provider.GetRequiredService<IReferenceImageGenerationService>());
        Assert.NotNull(provider.GetRequiredService<ThreeDGod.Rendering.ViewportSelectionService>());
    }

    [Fact]
    public void CoreProject_DoesNotContainBlenderServiceClass()
    {
        var coreDir = Path.Combine(RepoPaths.FindRepoRoot(), "3DGodCreator.Core");
        var leftover = Directory.GetFiles(coreDir, "BlenderService.cs", SearchOption.AllDirectories);
        Assert.Empty(leftover);
    }

    [Fact]
    public void CoreCsproj_DoesNotReferenceWpfOrHelix()
    {
        var csproj = File.ReadAllText(Path.Combine(RepoPaths.FindRepoRoot(), "3DGodCreator.Core", "3DGodCreator.Core.csproj"));
        Assert.DoesNotContain("UseWPF", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HelixToolkit", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PresentationCore", csproj, StringComparison.OrdinalIgnoreCase);
    }
}
