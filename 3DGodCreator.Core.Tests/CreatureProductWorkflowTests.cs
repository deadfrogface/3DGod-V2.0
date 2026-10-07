using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Persistence;
using Xunit;

namespace ThreeDGodCreator.Core.Tests;

public sealed class CreatureProductWorkflowTests
{
    [Theory]
    [InlineData("orc", 5)]
    [InlineData("rat", 5)]
    public async Task CreatureProject_IsAuthoritativeEmbeddedAndRoundtrips(string species, int expectedSceneParts)
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-creature-product-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var session = new ActiveProjectSession();
            var archive = new GodProjectArchive();
            var workflow = new ProductWorkflowService(
                session,
                archive,
                new AllowlistedAiEditExecutor(session),
                autosave: null,
                workRoot: root,
                creatures: new CreatureAssembly());

            if (species == "orc") workflow.NewOrcProject();
            else workflow.NewRatProject();

            var character = session.ActiveCharacter!;
            Assert.Equal(CharacterKind.HumanoidCreature, character.CharacterKind);
            Assert.Equal(species, character.CreatureState!.BaseFamily);

            var parts = session.ListSceneParts();
            Assert.Equal(expectedSceneParts, parts.Count);
            Assert.All(parts, p => Assert.True(session.Bundle.MeshBytes.ContainsKey(p.MeshAssetId)));

            var project = Path.Combine(root, species + ".3dgod");
            await workflow.SaveProjectAsync(project);

            var reopened = new ActiveProjectSession();
            var workflow2 = new ProductWorkflowService(
                reopened,
                archive,
                new AllowlistedAiEditExecutor(reopened),
                workRoot: root);
            await workflow2.OpenProjectAsync(project);

            Assert.Equal(species, reopened.ActiveCharacter!.CreatureState!.BaseFamily);
            Assert.Equal(expectedSceneParts, reopened.ListSceneParts().Count);

            var materialized = reopened.MaterializeSceneGlbs(Path.Combine(root, "scene-" + species));
            Assert.Equal(expectedSceneParts, materialized.Count);
            Assert.All(materialized, p => Assert.True(File.Exists(p.GlbPath)));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
