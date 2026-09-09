using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Rigging;

namespace ThreeDGodCreator.Core.Tests;

public class FreeformPipelineTests
{
    [Fact]
    public async Task KleinerDrache_RunsPipelineWithoutBlender()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-freeform-" + Guid.NewGuid().ToString("N"));
        var project = Path.Combine(root, "dragon.3dgod");
        Directory.CreateDirectory(root);
        try
        {
            var source = FreeformPipeline.BuildDragon();
            var sourceTris = source.Indices.Count / 3;
            Assert.True(sourceTris > 400);

            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "freeform" } };
            var pipeline = new FreeformPipeline(new ReferenceImageService(), new ImageTo3DService());
            var character = await pipeline.RunAsync("kleiner Drache", bundle, Path.Combine(root, "work"));

            Assert.Equal(CharacterKind.FreeformCreature, character.CharacterKind);
            Assert.Equal("procedural-catalog", character.GeneratedAssetMetadata!.BackendId);
            Assert.Equal("authored-freeform", character.GeneratedAssetMetadata.Parameters["rig"]);
            var mesh = bundle.Meshes.Single();
            Assert.True(File.Exists(mesh.CanonicalGlbPath));
            var doc = CanonicalGltfPipeline.Load(mesh.CanonicalGlbPath);
            Assert.True(doc.SkinCount > 0);
            Assert.True(doc.TriangleCount < sourceTris);
            Assert.True(doc.TriangleCount > 0);
            var report = RigValidator.ValidateGlb(mesh.CanonicalGlbPath, requireHumanoid: false);
            Assert.True(report.Passed, string.Join("; ", report.Failures.Select(f => f.Code)));
            Assert.Contains("head", report.JointNames);
            Assert.Contains("tail.base", report.JointNames);
            Assert.Contains(BoneSemanticTags.Head, character.CreatureState!.BodyPlan.CustomTags);
            Assert.Contains(BoneSemanticTags.TailBase, character.CreatureState.BodyPlan.CustomTags);

            await new GodProjectArchive().SaveAsync(bundle, project);
            var loaded = await new GodProjectArchive().LoadAsync(project);
            Assert.Equal(CharacterKind.FreeformCreature, loaded.Characters.Single().CharacterKind);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task UnknownPrompt_WritesNoMesh()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-freeform-miss-" + Guid.NewGuid().ToString("N"));
        var dest = Path.Combine(root, "rigged.glb");
        Directory.CreateDirectory(root);
        try
        {
            var pipeline = new FreeformPipeline(new ReferenceImageService(), new ImageTo3DService());
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                pipeline.RunAsync("drop table characters", new ProjectBundle(), root));
            Assert.Contains("NotInstalled", ex.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(dest));
            Assert.False(File.Exists(Path.Combine(root, "source.glb")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
