using System.Numerics;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;

namespace ThreeDGodCreator.Core.Tests;

public class RemeshPipelineTests
{
    [Fact]
    public void HighPolySphere_Preview_WritesLowerMeshWithUv()
    {
        var source = HighPolySphere();
        var sourceTris = source.Indices.Count / 3;
        Assert.True(sourceTris > 4000);

        var remeshed = RemeshPipeline.Run(source.Positions, source.Indices, RemeshProfile.Preview);
        var destTris = remeshed.Indices.Count / 3;
        Assert.True(destTris < sourceTris / 3);
        Assert.True(destTris >= 8);
        Assert.Equal(remeshed.Positions.Count, remeshed.Uvs.Count);
        AssertAllIndicesValid(remeshed.Positions, remeshed.Indices);
        var report = MeshValidator.Validate(remeshed.Positions, remeshed.Indices, remeshed.Uvs.Count);
        Assert.False(report.Rejected);
        Assert.DoesNotContain(report.Issues, i => i.Code == "InvalidIndex");
        Assert.All(remeshed.Uvs, uv =>
        {
            Assert.InRange(uv.X, 0f, 1f);
            Assert.InRange(uv.Y, 0f, 1f);
        });
        Assert.Equal("vertex-cluster", remeshed.BackendId);
    }

    [Fact]
    public void Profiles_ReduceInExpectedOrder()
    {
        var source = HighPolySphere();
        var keep = RemeshPipeline.Run(source.Positions, source.Indices, RemeshProfile.KeepOriginal);
        var character = RemeshPipeline.Run(source.Positions, source.Indices, RemeshProfile.CharacterCandidate);
        var game = RemeshPipeline.Run(source.Positions, source.Indices, RemeshProfile.StaticGameAsset);
        var preview = RemeshPipeline.Run(source.Positions, source.Indices, RemeshProfile.Preview);

        Assert.Equal(source.Indices.Count / 3, keep.Indices.Count / 3);
        Assert.True(character.Indices.Count < keep.Indices.Count);
        Assert.True(game.Indices.Count < character.Indices.Count);
        Assert.True(preview.Indices.Count < game.Indices.Count);
    }

    [Fact]
    public void RemeshGlb_HasUvAndNoInvalidIndices()
    {
        var source = HighPolySphere();
        var srcGlb = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-hi.glb");
        var dstGlb = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-lo.glb");
        try
        {
            TriangleMeshExport.WriteGlb(srcGlb, source.Positions, source.Indices);
            var service = new RemeshService();
            service.RemeshGlb(srcGlb, dstGlb, RemeshProfile.StaticGameAsset);
            var before = CanonicalGltfPipeline.Load(srcGlb);
            var after = CanonicalGltfPipeline.Load(dstGlb);
            Assert.True(after.HasUv);
            Assert.True(after.TriangleCount < before.TriangleCount);
            Assert.True(after.TriangleCount > 0);
            var (positions, indices) = MeshCompare.ReadMesh(dstGlb);
            AssertAllIndicesValid(positions, indices);
        }
        finally
        {
            if (File.Exists(srcGlb)) File.Delete(srcGlb);
            if (File.Exists(dstGlb)) File.Delete(dstGlb);
        }
    }

    [Fact]
    public void FeatureGate_Remesh_IsAvailable()
    {
        var features = new DynamicFeatureAvailabilityService();
        Assert.Equal(FeatureAvailability.Available, features.GetStatus(FeatureIds.Remesh));
        Assert.True(features.IsInvocable(FeatureIds.Remesh));
    }

    private static (List<Vector3> Positions, List<int> Indices) HighPolySphere()
    {
        var builder = new MeshBuilder3D();
        builder.AddSphere(Vector3.Zero, 1f, slices: 72, stacks: 48);
        return (builder.Positions, builder.Indices);
    }

    private static void AssertAllIndicesValid(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices)
    {
        Assert.True(indices.Count % 3 == 0);
        foreach (var index in indices)
        {
            Assert.InRange(index, 0, positions.Count - 1);
        }
    }
}
