using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;

namespace ThreeDGodCreator.Core.Tests;

public class SkinTokensTests
{
    [Fact]
    public void Probe_IsHonest_NoSuccess()
    {
        var hw = HardwareProfiler.Probe();
        var status = SkinTokensRuntime.Probe();
        Assert.NotEqual(FeatureAvailability.Available, status.Availability);
        Assert.DoesNotContain("success", status.Message, StringComparison.OrdinalIgnoreCase);
        if (!hw.Cuda || hw.VramMb < SkinTokensRuntime.MinVramMb)
            Assert.Equal(FeatureAvailability.UnsupportedHardware, status.Availability);
        else
            Assert.Equal(FeatureAvailability.NotInstalled, status.Availability);
        Assert.Equal(status.Availability, new DynamicFeatureAvailabilityService().GetStatus(FeatureIds.SkinTokens));
        Assert.False(new DynamicFeatureAvailabilityService().IsInvocable(FeatureIds.SkinTokens));
        Assert.Equal(FeatureAvailability.NotImplemented, new FeatureAvailabilityService().GetStatus(FeatureIds.RigAuto));
    }

    [Fact]
    public async Task Rig_WithoutHardware_WritesNoGlb()
    {
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-fake-skintokens-" + Guid.NewGuid().ToString("N") + ".glb");
        var src = Path.Combine(RepoPaths.FindRepoRoot(), "assets", "characters", "male_base.glb");
        if (!File.Exists(src))
            src = dest + ".src";
        try
        {
            var svc = new SkinTokensRigService();
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RigGlbAsync(src, dest));
            Assert.True(
                ex.Message.Contains("UnsupportedHardware", StringComparison.Ordinal) ||
                ex.Message.Contains("NotInstalled", StringComparison.Ordinal));
            Assert.False(File.Exists(dest));
        }
        finally
        {
            if (File.Exists(dest)) File.Delete(dest);
        }
    }

    [Fact]
    public void SemanticMap_MapsKnownBones_AndIgnoresUnknown()
    {
        var mapped = SemanticBoneMap.MapAll(["neck", "hand.R", "ear.L", "weapon.bone", "hips"]);
        Assert.Equal(BoneSemanticTags.Neck, mapped["neck"]);
        Assert.Equal(BoneSemanticTags.RightHand, mapped["hand.R"]);
        Assert.Equal(BoneSemanticTags.Head, mapped["ear.L"]);
        Assert.Equal(BoneSemanticTags.Hips, mapped["hips"]);
        Assert.False(mapped.ContainsKey("weapon.bone"));
        Assert.Null(SemanticBoneMap.TryMap(""));
    }
}
