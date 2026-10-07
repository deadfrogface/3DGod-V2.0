using System.Numerics;
using ThreeDGod.Application;
using ThreeDGod.AI;
using ThreeDGod.Core.Domain;
using ThreeDGod.Export;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Rigging;
using Xunit;

namespace ThreeDGodCreator.Core.Tests;

public sealed class CharacterCreatorHeadlessE2ETests
{
    [Fact]
    public async Task Orc_Edit_Gear_SaveReload_RigPose_ComposedExport_Ue5Preflight()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-character-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "E2E Orc" } };
            var orc = new CreatureAssembly().CreateOrc(bundle, Path.Combine(root, "orc"));
            var session = new ActiveProjectSession();
            session.LoadCreatedCreature(bundle, orc.CharacterId);

            var edits = new AllowlistedAiEditExecutor(session);
            var workflow = new ProductWorkflowService(session, new GodProjectArchive(), edits, workRoot: Path.Combine(root, "workflow"));
            var muscle = await edits.ExecutePlanAsync(
                DeterministicAiParser.ParseComposite("weniger muskulös").Single(),
                new ThreeDGod.Core.Editing.CommandStack());
            Assert.True(muscle.Ok);

            workflow.UpsertMaterial("skin", 0.2f, 0.35f, 0.15f, 0.1f, 0.7f);

            var weapon = Path.Combine(root, "weapon.glb");
            TriangleMeshExport.WriteGlb(weapon,
                [new Vector3(0,0,0), new Vector3(0.04f,0,0), new Vector3(0,0.5f,0)],
                [0,1,2]);
            session.AddAttachmentFromGlb(weapon, "E2E Weapon", AttachmentType.Weapon,
                new GeneratedAssetMetadata { BackendId = "headless-e2e", LicenseProfileId = "CC0-1.0" });

            var project = Path.Combine(root, "orc.3dgod");
            await workflow.SaveProjectAsync(project);
            Assert.True(File.Exists(project));

            var reopened = new ActiveProjectSession();
            var reopenedWorkflow = new ProductWorkflowService(reopened, new GodProjectArchive(), new AllowlistedAiEditExecutor(reopened), workRoot: Path.Combine(root, "reopened"));
            await reopenedWorkflow.OpenProjectAsync(project);
            Assert.Equal("orc", reopened.ActiveCharacter!.CreatureState!.BaseFamily);
            Assert.Contains(reopened.Bundle.Attachments, a => a.AttachmentType == AttachmentType.Weapon);

            var scene = reopened.MaterializeSceneGlbs(Path.Combine(root, "scene"));
            var body = scene.First(x => x.Name.Contains("orc-body", StringComparison.OrdinalIgnoreCase)).GlbPath;
            var rig = RigValidator.ValidateGlb(body, requireHumanoid: true);
            Assert.True(rig.Passed, string.Join("; ", rig.Failures.Select(x => x.Code)));
            var pose = TestPoseEvaluator.Evaluate(body, TestPoseKind.Arms);
            Assert.True(pose.Moved);

            var composed = reopenedWorkflow.ExportActiveGlb(Path.Combine(root, "orc-composed.glb"));
            Assert.True(File.Exists(composed));
            var doc = CanonicalGltfPipeline.Load(composed);
            Assert.True(doc.MeshCount >= 2);
            var ue5 = UnrealEngine5ExportProfile.EvaluateGlb(body, "E2E_Orc");
            Assert.True(ue5.Passed, string.Join("; ", ue5.HardMessages));
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }
}
