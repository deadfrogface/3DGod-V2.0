using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Rigging;

namespace ThreeDGodCreator.Core.Tests;

public class OrcCreatureTests
{
    [Fact]
    public async Task Orc_HasDistinctPartsMaterialRig_UndoAndSave()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-orc-" + Guid.NewGuid().ToString("N"));
        var project = Path.Combine(root, "orc.3dgod");
        Directory.CreateDirectory(root);
        try
        {
            var humanBundle = new ProjectBundle { Project = new ProjectDocument { Name = "human" } };
            var human = new CreatureAssembly().AttachHumanTailAndHorns(humanBundle, Path.Combine(root, "human"));
            var orcBundle = new ProjectBundle { Project = new ProjectDocument { Name = "orc" } };
            var orc = new CreatureAssembly().CreateOrc(orcBundle, Path.Combine(root, "orc"));

            Assert.Equal("orc", orc.CreatureState!.BaseFamily);
            Assert.Equal(4, orc.CreatureState.ExtraBodyParts.Count);
            Assert.Equal(2, orc.CreatureState.ExtraBodyParts.Count(p => p.SemanticType == SemanticBodyPartType.Ear));
            Assert.Equal(2, orc.CreatureState.ExtraBodyParts.Count(p => p.SemanticType == SemanticBodyPartType.Tusk));
            Assert.DoesNotContain(orc.CreatureState.ExtraBodyParts, p => p.SemanticType == SemanticBodyPartType.Horn);
            Assert.Equal(0.92f, orc.ParametricHumanState!.PhenotypeParameters["muscle"]);

            var orcSkin = orcBundle.Materials.Single();
            var humanSkin = PbrMaterials.Get("Skin");
            Assert.Equal("OrcSkin", orcSkin.Name);
            Assert.NotEqual(humanSkin.BaseColor.X, (float)orcSkin.BaseColorFactor.R, 2);

            var body = orcBundle.Meshes.Single(m => m.Name == "orc-body");
            Assert.True(body.HasSkin);
            var rigReport = RigValidator.ValidateGlb(body.CanonicalGlbPath);
            Assert.True(rigReport.Passed, string.Join("; ", rigReport.Failures.Select(f => f.Code)));

            var horn = humanBundle.Meshes.Single(m => m.Name == "horn.L");
            var tusk = orcBundle.Meshes.Single(m => m.Name == "tusk.L");
            Assert.NotEqual(horn.TriangleCount, tusk.TriangleCount);
            var hornPos = MeshCompare.ReadPositions(horn.CanonicalGlbPath);
            var tuskPos = MeshCompare.ReadPositions(tusk.CanonicalGlbPath);
            Assert.False(MeshCompare.IsUniformScale(hornPos, tuskPos));

            var extras = orc.CreatureState.ExtraBodyParts;
            var snapshot = extras.ToList();
            var stack = new CommandStack();
            stack.BeginTransaction();
            foreach (var part in snapshot)
                await stack.ExecuteAsync(new CollectionChangeCommand<BodyPartSlot>(
                    orc.CharacterId, extras, part, add: false, "creature.remove-part"));
            await stack.CommitTransactionAsync();
            Assert.Empty(extras);
            await stack.UndoAsync();
            Assert.Equal(4, extras.Count);

            await new GodProjectArchive().SaveAsync(orcBundle, project);
            var loaded = await new GodProjectArchive().LoadAsync(project);
            var copy = loaded.Characters.Single();
            Assert.Equal("Orc", copy.Name);
            Assert.Equal(4, copy.CreatureState!.ExtraBodyParts.Count);
            Assert.Equal("OrcSkin", loaded.Materials.Single().Name);
            Assert.Contains(loaded.Meshes, m => m.HasSkin && m.Name == "orc-body");
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
