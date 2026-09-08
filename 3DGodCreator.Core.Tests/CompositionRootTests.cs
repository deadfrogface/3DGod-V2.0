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

        Assert.Null(provider.GetService<IProjectService>());
        Assert.Null(provider.GetService<IWorkerHost>());
        Assert.Null(provider.GetService<IBackendRegistry>());
        Assert.Null(provider.GetService<IImportService>());
        Assert.Null(provider.GetService<IExportService>());
        Assert.Null(provider.GetService<IRiggingService>());
        Assert.Null(provider.GetService<IImageTo3DService>());
    }
}
