using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Persistence;
using ThreeDGod.Rigging;

namespace ThreeDGodCreator.Core.Tests;

public class RatCreatureTests
{
    [Fact]
    public async Task HumanoidRat_SavesTail_AndHeadPoseKeepsPartsAttached()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-rat-" + Guid.NewGuid().ToString("N"));
        var project = Path.Combine(root, "rat.3dgod");
        Directory.CreateDirectory(root);
        try
        {
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "rat" } };
            var rat = new CreatureAssembly().CreateRat(bundle, Path.Combine(root, "parts"));
            Assert.Equal("rat", rat.CreatureState!.BaseFamily);
            Assert.Equal(1, rat.CreatureState.BodyPlan.TailCount);
            Assert.Contains(rat.CreatureState.ExtraBodyParts, p => p.SemanticType == SemanticBodyPartType.Head);
            Assert.Equal(2, rat.CreatureState.ExtraBodyParts.Count(p => p.SemanticType == SemanticBodyPartType.Ear));
            Assert.Contains(rat.CreatureState.ExtraBodyParts, p =>
                p.SemanticType == SemanticBodyPartType.Tail && p.ParentBoneSemantic == "tail.base");

            var body = bundle.Meshes.Single(m => m.Name == "rat-body");
            Assert.True(RigValidator.ValidateGlb(body.CanonicalGlbPath).Passed);

            var headRest = new SpatialTransform { Tx = 0, Ty = 1.7, Tz = 0.05 };
            var headPosed = AttachmentKinematics.RotateY(headRest, 35);
            foreach (var part in rat.CreatureState.ExtraBodyParts.Where(p => p.ParentBoneSemantic == "head"))
            {
                var bind = new AttachmentInstance
                {
                    CharacterId = rat.CharacterId,
                    AssetId = part.MeshAssetId ?? Guid.Empty,
                    ParentBoneSemantic = part.ParentBoneSemantic,
                    LocalTransform = part.LocalTransform
                };
                var worldRest = AttachmentKinematics.WorldOnPose(bind, headRest);
                var worldPosed = AttachmentKinematics.WorldOnPose(bind, headPosed);
                Assert.NotEqual(worldRest.Tx, worldPosed.Tx);
                Assert.Equal(worldRest.Ty, worldPosed.Ty, 5);
            }

            await new GodProjectArchive().SaveAsync(bundle, project);
            var loaded = await new GodProjectArchive().LoadAsync(project);
            var copy = loaded.Characters.Single();
            Assert.Equal("Humanoid Rat", copy.Name);
            Assert.Equal(1, copy.CreatureState!.BodyPlan.TailCount);
            Assert.Contains(copy.CreatureState.ExtraBodyParts, p => p.SemanticType == SemanticBodyPartType.Tail);
            Assert.Contains(copy.CreatureState.BodyPlan.CustomTags, t => t.Contains("rat", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
