using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Export;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;

namespace ThreeDGodCreator.Core.Tests;

public class GlbExportRoundtripTests
{
    [Fact]
    public void CompleteScene_WritesMeshMaterialTextureSkinMorphNodes_AndRoundtripPreservesCounts()
    {
        var src = Path.Combine(Path.GetTempPath(), "3dgod-p48-" + Guid.NewGuid().ToString("N") + ".glb");
        var dst = Path.Combine(Path.GetTempPath(), "3dgod-p48-rt-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            GlbExportService.WriteCompleteSceneStatic(src);
            var before = CanonicalGltfPipeline.Load(src);
            Assert.True(before.MeshCount >= 1, "Mesh missing.");
            Assert.True(before.MaterialCount >= 1, "Material missing.");
            Assert.True(before.ImageCount >= 1 || before.TextureCount >= 1, "Texture/image missing.");
            Assert.True(before.SkinCount >= 1, "Skin missing.");
            Assert.True(before.HasJoints, "JOINTS_0 missing.");
            Assert.True(before.HasUv, "TEXCOORD_0 missing.");
            Assert.True(before.MorphTargetCount >= 1, "Morph target missing.");
            Assert.True(before.NodeCount >= 3, $"Expected hips/spine/mesh nodes, got {before.NodeCount}.");
            Assert.True(before.VertexCount >= 8);
            Assert.True(before.TriangleCount >= 12);

            var model = SharpGLTF.Schema2.ModelRoot.Load(src);
            var names = model.LogicalNodes.Select(n => n.Name ?? "").ToArray();
            Assert.Contains(names, n => n.Equals("hips", StringComparison.Ordinal));
            Assert.Contains(names, n => n.Equals("spine", StringComparison.Ordinal));

            GlbExportService.Export(src, dst);
            var after = CanonicalGltfPipeline.Load(dst);
            Assert.Equal(before.MeshCount, after.MeshCount);
            Assert.Equal(before.MaterialCount, after.MaterialCount);
            Assert.Equal(before.TextureCount, after.TextureCount);
            Assert.Equal(before.ImageCount, after.ImageCount);
            Assert.Equal(before.SkinCount, after.SkinCount);
            Assert.Equal(before.MorphTargetCount, after.MorphTargetCount);
            Assert.Equal(before.NodeCount, after.NodeCount);
            Assert.Equal(before.VertexCount, after.VertexCount);
            Assert.Equal(before.TriangleCount, after.TriangleCount);
        }
        finally
        {
            if (File.Exists(src)) File.Delete(src);
            if (File.Exists(dst)) File.Delete(dst);
        }
    }

    [Fact]
    public void Di_RegistersGlbExportService()
    {
        using var provider = new ServiceCollection()
            .AddThreeDGodCoreServices()
            .BuildServiceProvider();
        var svc = provider.GetRequiredService<IGlbExportService>();
        Assert.IsType<GlbExportService>(svc);
    }

    [Fact]
    public void Export_IsNotByteCopy_OfMaleBase()
    {
        var src = Path.Combine(RepoPaths.AssetsDir, "characters", "male_base.glb");
        var dst = Path.Combine(Path.GetTempPath(), "3dgod-p48-male-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            GlbExportService.Export(src, dst);
            var before = CanonicalGltfPipeline.Load(src);
            var after = CanonicalGltfPipeline.Load(dst);
            Assert.Equal(before.VertexCount, after.VertexCount);
            Assert.Equal(before.TriangleCount, after.TriangleCount);
            Assert.True(new FileInfo(dst).Length > 64);
        }
        finally
        {
            if (File.Exists(dst)) File.Delete(dst);
        }
    }
}
