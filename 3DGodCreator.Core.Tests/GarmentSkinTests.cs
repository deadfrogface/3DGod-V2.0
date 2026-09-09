using ThreeDGod.Application;
using ThreeDGod.Infrastructure;
using ThreeDGod.Rigging;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

public class GarmentSkinTests
{
    [Fact]
    public void IdentityBind_ArmsAndElbowMove()
    {
        var body = Path.Combine(Path.GetTempPath(), "3dgod-gs-body-" + Guid.NewGuid().ToString("N") + ".glb");
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-gs-id-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            HumanoidTestRig.WriteGood(body);
            new GarmentSkinService().BindToBody(body, body, dest);
            var arms = TestPoseEvaluator.Evaluate(dest, TestPoseKind.Arms);
            var elbow = TestPoseEvaluator.Evaluate(dest, TestPoseKind.Elbow);
            Assert.True(arms.Moved, $"Arms did not move the bound mesh (delta={arms.MaxVertexDelta}).");
            Assert.True(elbow.Moved, $"Elbow did not move the bound mesh (delta={elbow.MaxVertexDelta}).");
        }
        finally
        {
            if (File.Exists(body)) File.Delete(body);
            if (File.Exists(dest)) File.Delete(dest);
        }
    }

    [SkippableFact]
    public async Task FittedJacket_FollowsArmAndElbowPose()
    {
        var gc = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        if (gc.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("GarmentCode runtime missing; fitted jacket skin pose test not executed.");
            return;
        }

        var work = Path.Combine(Path.GetTempPath(), "3dgod-gs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var body = Path.Combine(work, "body.glb");
            HumanoidTestRig.WriteGood(body);
            await using var garment = new GarmentCodeService(new WorkerProcessHost());
            var fit = new GarmentFitService(new AnnyHumanService(new WorkerProcessHost()), garment);
            var fitted = await fit.FitJacketToBodyAsync(body, work, "rig-humanoid");
            var skinned = Path.Combine(work, "jacket-skinned.glb");
            new GarmentSkinService().BindToBody(body, fitted.FittedGlb, skinned);

            var rest = TestPoseEvaluator.SkinAtPose(skinned, "upperarm.L", 0);
            var arms = TestPoseEvaluator.Evaluate(skinned, TestPoseKind.Arms, 70f);
            var elbow = TestPoseEvaluator.Evaluate(skinned, TestPoseKind.Elbow, 80f);
            Assert.True(arms.Moved, $"Jacket did not follow arm raise (delta={arms.MaxVertexDelta}).");
            Assert.True(elbow.Moved, $"Jacket did not follow elbow bend (delta={elbow.MaxVertexDelta}).");
            Assert.True(arms.MaxVertexDelta < 2f, $"Catastrophic detach: arm delta={arms.MaxVertexDelta}.");
            Assert.True(elbow.MaxVertexDelta < 2f, $"Catastrophic detach: elbow delta={elbow.MaxVertexDelta}.");
            Assert.Equal(rest.Count, TestPoseEvaluator.SkinAtPose(skinned, "upperarm.L", 70f).Count);
        }
        finally
        {
            if (Directory.Exists(work))
                Directory.Delete(work, recursive: true);
        }
    }

    [Fact]
    public void FeatureGate_IsExperimental()
    {
        var features = new DynamicFeatureAvailabilityService();
        Assert.Equal(FeatureAvailability.Experimental, features.GetStatus(FeatureIds.GarmentSkin));
        Assert.True(features.IsInvocable(FeatureIds.GarmentSkin));
        Assert.DoesNotContain("success", features.GetStatusMessage(FeatureIds.GarmentSkin), StringComparison.OrdinalIgnoreCase);
    }
}
