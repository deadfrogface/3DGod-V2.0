using System.Numerics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;

namespace ThreeDGodCreator.Core.Tests;

public class SharpGltfSmokeTests
{
    [Fact]
    public void Toolkit_RoundtripsMinimalTriangleGlb()
    {
        var material = new MaterialBuilder("smoke")
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader()
            .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(0.8f, 0.2f, 0.2f, 1));

        var mesh = new MeshBuilder<VertexPosition>("triangle");
        var primitive = mesh.UsePrimitive(material);
        primitive.AddTriangle(
            new VertexPosition(0, 0, 0),
            new VertexPosition(1, 0, 0),
            new VertexPosition(0, 1, 0));

        var scene = new SceneBuilder();
        scene.AddRigidMesh(mesh, Matrix4x4.Identity);

        var path = Path.Combine(Path.GetTempPath(), "3dgod-phase00-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            scene.ToGltf2().SaveGLB(path);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 0);

            var loaded = ModelRoot.Load(path);
            Assert.NotNull(loaded);
            Assert.True(loaded.LogicalMeshes.Count >= 1);
            Assert.True(loaded.LogicalMeshes[0].Primitives.Count >= 1);

            var positions = loaded.LogicalMeshes[0].Primitives[0].GetVertexAccessor("POSITION");
            Assert.NotNull(positions);
            Assert.Equal(3, positions!.AsVector3Array().Count);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Theory]
    [InlineData("male_base.glb")]
    [InlineData("female_base.glb")]
    public void ProductBaseGlb_LoadsWithMeshes_AndHasNoSkin(string fileName)
    {
        var path = Path.Combine(RepoPaths.AssetsDir, "characters", fileName);
        Assert.True(File.Exists(path), path + " missing.");

        var model = ModelRoot.Load(path);
        Assert.NotNull(model);
        Assert.True(model.LogicalMeshes.Count > 0, fileName + " has no meshes.");
        Assert.True(model.LogicalMeshes[0].Primitives.Count > 0);

        var positions = model.LogicalMeshes[0].Primitives[0].GetVertexAccessor("POSITION");
        Assert.NotNull(positions);
        Assert.True(positions!.AsVector3Array().Count > 0);

        var skinCount = model.LogicalSkins?.Count ?? 0;
        Assert.Equal(0, skinCount);
    }
}
