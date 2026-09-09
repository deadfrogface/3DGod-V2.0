using ThreeDGod.AI;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;

namespace ThreeDGodCreator.Core.Tests;

public class CreatureTextEditTests
{
    [Theory]
    [InlineData("Rattenkopf.", "creature.replacePart", "head")]
    [InlineData("Hörner.", "creature.addPart", "horn")]
    [InlineData("rechte Hand mechanisch.", "creature.replacePart", "rightHand")]
    public void GermanPrompts_ParseToCreatureOps(string prompt, string operation, string slot)
    {
        var plan = DeterministicAiParser.Parse(prompt);
        Assert.Equal("valid", plan.Status);
        Assert.Equal(operation, plan.Operation);
        Assert.Equal(slot, plan.Args["slot"]);
        Assert.True(plan.Args["op"] is "ReplaceBodyPart" or "AddCreaturePart");
    }

    [Fact]
    public async Task RatHead_ChangesOnlyHeadRegion_AndUndoRestores()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-edit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "edit" } };
            var character = new CreatureAssembly().CreateEditableHumanoid(bundle, Path.Combine(root, "base"));
            var extras = character.CreatureState!.ExtraBodyParts;
            var oldHead = extras.Single(p => p.SemanticType == SemanticBodyPartType.Head);
            var oldLeft = extras.Single(p => p.SemanticType == SemanticBodyPartType.LeftHand);
            var oldRight = extras.Single(p => p.SemanticType == SemanticBodyPartType.RightHand);
            var stack = new CommandStack();
            await new CreatureTextEditService().ApplyAsync("Rattenkopf.", bundle, character, Path.Combine(root, "rat"), stack);

            Assert.DoesNotContain(extras, p => p.SlotId == oldHead.SlotId);
            Assert.Contains(extras, p => p.SemanticType == SemanticBodyPartType.Head);
            Assert.Equal(oldLeft.SlotId, extras.Single(p => p.SemanticType == SemanticBodyPartType.LeftHand).SlotId);
            Assert.Equal(oldRight.SlotId, extras.Single(p => p.SemanticType == SemanticBodyPartType.RightHand).SlotId);
            var newHead = extras.Single(p => p.SemanticType == SemanticBodyPartType.Head);
            var oldMesh = bundle.Meshes.Single(m => m.MeshAssetId == oldHead.MeshAssetId);
            var newMesh = bundle.Meshes.Single(m => m.MeshAssetId == newHead.MeshAssetId);
            Assert.NotEqual(oldMesh.TriangleCount, newMesh.TriangleCount);
            Assert.False(MeshCompare.IsUniformScale(
                MeshCompare.ReadPositions(oldMesh.CanonicalGlbPath),
                MeshCompare.ReadPositions(newMesh.CanonicalGlbPath)));

            await stack.UndoAsync();
            Assert.Contains(extras, p => p.SlotId == oldHead.SlotId);
            Assert.Equal(oldHead.MeshAssetId, extras.Single(p => p.SemanticType == SemanticBodyPartType.Head).MeshAssetId);
            Assert.DoesNotContain(extras, p => p.SemanticType == SemanticBodyPartType.Ear);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Horns_AddWithoutTouchingHands_AndUndoRemoves()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-horns-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "horns" } };
            var character = new CreatureAssembly().CreateEditableHumanoid(bundle, Path.Combine(root, "base"));
            var extras = character.CreatureState!.ExtraBodyParts;
            var headId = extras.Single(p => p.SemanticType == SemanticBodyPartType.Head).SlotId;
            var rightId = extras.Single(p => p.SemanticType == SemanticBodyPartType.RightHand).SlotId;
            var stack = new CommandStack();
            await new CreatureTextEditService().ApplyAsync("Hörner.", bundle, character, Path.Combine(root, "horns"), stack);
            Assert.Equal(2, extras.Count(p => p.SemanticType == SemanticBodyPartType.Horn));
            Assert.Equal(headId, extras.Single(p => p.SemanticType == SemanticBodyPartType.Head).SlotId);
            Assert.Equal(rightId, extras.Single(p => p.SemanticType == SemanticBodyPartType.RightHand).SlotId);
            await stack.UndoAsync();
            Assert.DoesNotContain(extras, p => p.SemanticType == SemanticBodyPartType.Horn);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task MechanicalRightHand_LeavesLeftHand_AndUndoRestores()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-hand-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "hand" } };
            var character = new CreatureAssembly().CreateEditableHumanoid(bundle, Path.Combine(root, "base"));
            var extras = character.CreatureState!.ExtraBodyParts;
            var oldRight = extras.Single(p => p.SemanticType == SemanticBodyPartType.RightHand);
            var leftId = extras.Single(p => p.SemanticType == SemanticBodyPartType.LeftHand).SlotId;
            var headId = extras.Single(p => p.SemanticType == SemanticBodyPartType.Head).SlotId;
            var stack = new CommandStack();
            await new CreatureTextEditService().ApplyAsync("rechte Hand mechanisch.", bundle, character, Path.Combine(root, "mech"), stack);
            var nowRight = extras.Single(p => p.SemanticType == SemanticBodyPartType.RightHand);
            Assert.NotEqual(oldRight.SlotId, nowRight.SlotId);
            Assert.Equal("boundary:wrist.R.prosthetic", nowRight.BoundaryDefinition);
            Assert.Equal(leftId, extras.Single(p => p.SemanticType == SemanticBodyPartType.LeftHand).SlotId);
            Assert.Equal(headId, extras.Single(p => p.SemanticType == SemanticBodyPartType.Head).SlotId);
            var oldPos = MeshCompare.ReadPositions(bundle.Meshes.Single(m => m.MeshAssetId == oldRight.MeshAssetId).CanonicalGlbPath);
            var newPos = MeshCompare.ReadPositions(bundle.Meshes.Single(m => m.MeshAssetId == nowRight.MeshAssetId).CanonicalGlbPath);
            Assert.NotEqual(oldPos.Count, newPos.Count);
            await stack.UndoAsync();
            Assert.Equal(oldRight.SlotId, extras.Single(p => p.SemanticType == SemanticBodyPartType.RightHand).SlotId);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
