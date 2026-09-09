using ThreeDGod.Application;
using ThreeDGod.Infrastructure;

namespace ThreeDGodCreator.Core.Tests;

public class GenerativeGarmentDecisionTests
{
    [Fact]
    public void DecisionDocument_Exists()
    {
        var path = Path.Combine(RepoPaths.FindRepoRoot(), "docs", "research", "GENERATIVE_GARMENT_DECISION.md");
        Assert.True(File.Exists(path), "PHASE 45 requires docs/research/GENERATIVE_GARMENT_DECISION.md");
        var text = File.ReadAllText(path);
        Assert.Contains("NotImplemented", text, StringComparison.Ordinal);
        Assert.Contains("DressCode", text, StringComparison.Ordinal);
        Assert.Contains("GarmentDiffusion", text, StringComparison.Ordinal);
        Assert.Contains("Garment3DGen", text, StringComparison.Ordinal);
        Assert.Contains("Kein Provider", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerativeGarment_IsNotImplemented_NoFakeButton()
    {
        var staticSvc = new FeatureAvailabilityService();
        Assert.Equal(FeatureAvailability.NotImplemented, staticSvc.GetStatus(FeatureIds.GenerativeGarment));
        Assert.False(staticSvc.IsInvocable(FeatureIds.GenerativeGarment));

        var dyn = new DynamicFeatureAvailabilityService();
        Assert.Equal(FeatureAvailability.NotImplemented, dyn.GetStatus(FeatureIds.GenerativeGarment));
        Assert.False(dyn.IsInvocable(FeatureIds.GenerativeGarment));
        Assert.Contains("NotImplemented", dyn.GetStatusMessage(FeatureIds.GenerativeGarment), StringComparison.Ordinal);
        Assert.DoesNotContain("success", dyn.GetStatusMessage(FeatureIds.GenerativeGarment), StringComparison.OrdinalIgnoreCase);
    }
}
