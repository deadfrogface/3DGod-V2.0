using System.Numerics;
using ThreeDGod.Mesh;

namespace ThreeDGodCreator.Core.Tests;

public class MeshPipelineTests
{
    [Theory]
    [InlineData("male_base.glb")]
    [InlineData("female_base.glb")]
    public void CanonicalGltf_Roundtrip_PreservesCounts(string file)
    {
        var src = Path.Combine(RepoPaths.AssetsDir, "characters", file);
        var original = CanonicalGltfPipeline.Load(src);
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            var round = CanonicalGltfPipeline.Roundtrip(src, dst);
            Assert.Equal(original.MeshCount, round.MeshCount);
            Assert.Equal(original.PrimitiveCount, round.PrimitiveCount);
            Assert.Equal(original.VertexCount, round.VertexCount);
            Assert.Equal(original.SkinCount, round.SkinCount);
            Assert.Equal(original.MaterialCount, round.MaterialCount);
        }
        finally
        {
            if (File.Exists(dst)) File.Delete(dst);
        }
    }

    [Fact]
    public void ObjImport_LoadsTriangleWithoutCrash()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".obj");
        File.WriteAllText(path, "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");
        try
        {
            var mesh = AssimpImportGate.Import(path);
            Assert.Equal(3, mesh.Positions.Count);
            Assert.Equal(3, mesh.Indices.Count);
            Assert.Equal("obj", mesh.SourceFormat);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void UnknownFormat_IsNotInstalled_NotSuccess()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => AssimpImportGate.Import("file.fbx"));
        Assert.Contains("NotInstalled", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MeshValidator_HardErrorsReject_SoftWarn()
    {
        var nan = MeshValidator.Validate([new Vector3(float.NaN, 0, 0)], [0, 0, 0], uvCount: 1);
        Assert.True(nan.Rejected);
        var badIdx = MeshValidator.Validate([Vector3.Zero], [0, 1, 2], 1);
        Assert.True(badIdx.Rejected);
        var noUv = MeshValidator.Validate([Vector3.Zero, Vector3.UnitX, Vector3.UnitY], [0, 1, 2], uvCount: 0);
        Assert.False(noUv.Rejected);
        Assert.Contains(noUv.Issues, i => i.Code == "NoUv");
        var disc = MeshValidator.Validate([Vector3.Zero, Vector3.UnitX, Vector3.UnitY], [0, 1, 2], 1, connectedComponents: 2);
        Assert.Contains(disc.Issues, i => i.Code == "Disconnected");
    }
}
