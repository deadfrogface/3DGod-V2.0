using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure;
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
        Assert.NotNull(provider.GetRequiredService<AnnyPresetStore>());
        Assert.NotNull(provider.GetService<IBackendRegistry>());
        Assert.Null(provider.GetService<IImportService>());
        Assert.Null(provider.GetService<IExportService>());
        Assert.Null(provider.GetService<IRiggingService>());
        Assert.Null(provider.GetService<IImageTo3DService>());
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
