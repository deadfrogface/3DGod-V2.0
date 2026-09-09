using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Persistence;

namespace ThreeDGodCreator.Core.Tests;

public class ImageTo3DTests
{
    [Theory]
    [InlineData("triposr")]
    [InlineData("sf3d")]
    [InlineData("spar3d")]
    [InlineData("trellis")]
    public void Probe_IsNotAvailable_AndNeverSuccess(string backend)
    {
        var status = ImageTo3DRuntime.Probe(backend);
        Assert.NotEqual(FeatureAvailability.Available, status.Availability);
        Assert.DoesNotContain("success", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Generate_WithoutRuntime_ThrowsAndWritesNoGlb()
    {
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-fake-triposr-" + Guid.NewGuid().ToString("N") + ".glb");
        var png = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(png, Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="));
        try
        {
            var svc = new ImageTo3DService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.GenerateGlbAsync(png, dest, "triposr"));
            Assert.Contains("NotInstalled", ex.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(dest));
        }
        finally
        {
            if (File.Exists(png)) File.Delete(png);
            if (File.Exists(dest)) File.Delete(dest);
        }
    }

    [SkippableFact]
    public async Task ExistingGlb_CanBeAttached_AndSavedInProject()
    {
        var src = Path.Combine(RepoPaths.FindRepoRoot(), "assets", "characters", "male_base.glb");
        if (!File.Exists(src))
        {
            TestGate.ExternalDependency("test asset male_base.glb missing; attach/save path not executed.");
            return;
        }
        var project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".3dgod");
        try
        {
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "i23" } };
            var asset = new ImageTo3DService().AttachExistingGlb(src, bundle);
            Assert.Equal("glb", asset.SourceFormat);
            Assert.Contains(asset.MeshAssetId, bundle.Project.AssetIds);
            await new GodProjectArchive().SaveAsync(bundle, project);
            var loaded = await new GodProjectArchive().LoadAsync(project);
            Assert.Contains(loaded.Meshes, m => m.MeshAssetId == asset.MeshAssetId);
        }
        finally
        {
            if (File.Exists(project)) File.Delete(project);
            if (File.Exists(project + ".bak")) File.Delete(project + ".bak");
        }
    }
}
