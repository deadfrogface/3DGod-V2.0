using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Rigging;

namespace ThreeDGodCreator.Core.Tests;

public class RigValidatorTests
{
    [Fact]
    public void KnownGoodRig_Passes_AndTestPosesMoveVertices()
    {
        var path = Path.Combine(Path.GetTempPath(), "3dgod-good-rig-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            HumanoidTestRig.WriteGood(path);
            var report = RigValidator.ValidateGlb(path);
            Assert.True(report.Passed, string.Join("; ", report.Failures.Select(f => f.Code + ":" + f.Message)));
            Assert.True(report.SkinCount > 0);
            Assert.Contains("hips", report.JointNames);
            Assert.Contains("tail.base", report.JointNames);

            foreach (var pose in Enum.GetValues<TestPoseKind>())
            {
                var result = TestPoseEvaluator.Evaluate(path, pose);
                Assert.True(result.Moved, $"{pose} did not move vertices (delta={result.MaxVertexDelta}).");
            }

            var svc = new RigValidationService();
            Assert.True(svc.ValidateGlb(path, out var failures));
            Assert.Empty(failures);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void UnskinnedMesh_FailsNoSkin()
    {
        var path = Path.Combine(Path.GetTempPath(), "3dgod-bad-rig-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            HumanoidTestRig.WriteUnskinned(path);
            var report = RigValidator.ValidateGlb(path);
            Assert.False(report.Passed);
            Assert.Contains(report.Failures, f => f.Code == "NoSkin");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void CyclicHierarchy_Fails()
    {
        var a = new BoneDefinition { BoneId = "a", Name = "hips", ParentBoneId = "b" };
        var b = new BoneDefinition { BoneId = "b", Name = "spine", ParentBoneId = "a" };
        var report = RigValidator.ValidateHierarchy([a, b]);
        Assert.False(report.Passed);
        Assert.Contains(report.Failures, f => f.Code == "HierarchyCycle");
    }

    [Fact]
    public void FeatureGate_RigValidate_IsAvailable()
    {
        var features = new DynamicFeatureAvailabilityService();
        Assert.Equal(FeatureAvailability.Available, features.GetStatus(FeatureIds.RigValidate));
        Assert.True(features.IsInvocable(FeatureIds.RigValidate));
    }
}
