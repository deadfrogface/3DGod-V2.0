using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;

namespace ThreeDGodCreator.Core.Tests;

public class CreatureAssemblyTests
{
    [Fact]
    public async Task HumanTailAndHorns_SurviveProjectSaveReload()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-creature-" + Guid.NewGuid().ToString("N"));
        var project = Path.Combine(root, "human-tail-horns.3dgod");
        Directory.CreateDirectory(root);
        try
        {
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "creature" } };
            var character = new CreatureAssembly().AttachHumanTailAndHorns(bundle, Path.Combine(root, "parts"));
            Assert.Equal(1, character.CreatureState!.BodyPlan.TailCount);
            Assert.Equal(3, character.CreatureState.ExtraBodyParts.Count);
            Assert.Contains(character.CreatureState.ExtraBodyParts, p =>
                p.SemanticType == SemanticBodyPartType.Tail && p.ParentBoneSemantic == "tail.base");
            Assert.Equal(2, character.CreatureState.ExtraBodyParts.Count(p => p.SemanticType == SemanticBodyPartType.Horn));
            Assert.All(character.CreatureState.ExtraBodyParts, p =>
            {
                Assert.False(string.IsNullOrWhiteSpace(p.BoundaryDefinition));
                Assert.NotNull(p.MeshAssetId);
            });
            Assert.Contains("skeleton:tail.base", character.CreatureState.BodyPlan.CustomTags);
            foreach (var mesh in bundle.Meshes)
            {
                Assert.True(File.Exists(mesh.CanonicalGlbPath));
                var doc = CanonicalGltfPipeline.Load(mesh.CanonicalGlbPath);
                Assert.True(doc.TriangleCount >= 8);
            }

            await new GodProjectArchive().SaveAsync(bundle, project);
            var loaded = await new GodProjectArchive().LoadAsync(project);
            var copy = loaded.Characters.Single(c => c.CharacterId == character.CharacterId);
            Assert.Equal(3, copy.CreatureState!.ExtraBodyParts.Count);
            Assert.Equal(1, copy.CreatureState.BodyPlan.TailCount);
            Assert.Equal("humanoid+tail", copy.CreatureState.SkeletonProfileId);
            Assert.Contains(copy.CreatureState.ExtraBodyParts, p => p.SemanticType == SemanticBodyPartType.Tail);
            Assert.Equal(2, copy.CreatureState.ExtraBodyParts.Count(p => p.ParentBoneSemantic == "head"));
            Assert.Equal(3, loaded.Meshes.Count);
            Assert.All(copy.CreatureState.ExtraBodyParts, p => Assert.Contains(loaded.Meshes, m => m.MeshAssetId == p.MeshAssetId));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
