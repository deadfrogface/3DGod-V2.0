using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class CompositionRootTests
{
    [Fact]
    public void AddThreeDGodCoreServices_ResolvesRealCharacterAndBlenderHost()
    {
        var provider = new ServiceCollection()
            .AddThreeDGodCoreServices()
            .BuildServiceProvider();

        var character = provider.GetService<CharacterSystem>();
        var blender = provider.GetService<IBlenderOperations>();
        var model = provider.GetService<ICharacterModelService>();

        Assert.NotNull(character);
        Assert.NotNull(blender);
        Assert.NotNull(model);
        Assert.IsType<LegacyBlenderBackend>(blender);
        Assert.IsType<CharacterModelServiceAdapter>(model);
    }

    [Fact]
    public void AddThreeDGodCoreServices_DoesNotRegisterUnimplementedCapabilities()
    {
        var provider = new ServiceCollection()
            .AddThreeDGodCoreServices()
            .BuildServiceProvider();

        Assert.NotNull(provider.GetService<IProjectService>());
        Assert.NotNull(provider.GetService<IWorkerHost>());
        Assert.NotNull(provider.GetService<IBackendRegistry>());
        // Honest NotInstalled / facade gates — not silent nulls pretending the capability is absent from the product surface.
        var import = provider.GetService<IImportService>();
        var export = provider.GetService<IExportService>();
        var rigging = provider.GetService<IRiggingService>();
        Assert.NotNull(import);
        Assert.NotNull(export);
        Assert.NotNull(rigging);
        Assert.Equal(FeatureAvailability.NotInstalled, import!.Probe());
        Assert.Equal(FeatureAvailability.Available, export!.Probe());
        Assert.Equal(FeatureAvailability.NotInstalled, rigging!.Probe());
        Assert.NotNull(provider.GetService<IAutoRigBackend>());
        Assert.NotNull(provider.GetService<IImageTo3DService>());
    }
}
