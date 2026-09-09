using ThreeDGod.AI;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure;

namespace ThreeDGodCreator.Core.Tests;

public class LocalAiEditDecisionTests
{
    [Fact]
    public void DecisionDocument_ExistsAndRejectsProviders()
    {
        var path = Path.Combine(RepoPaths.FindRepoRoot(), "docs", "research", "LOCAL_AI_EDIT_DECISION.md");
        Assert.True(File.Exists(path), "PHASE 47 requires docs/research/LOCAL_AI_EDIT_DECISION.md");
        var text = File.ReadAllText(path);
        Assert.Contains("NotImplemented", text, StringComparison.Ordinal);
        Assert.Contains("BlendedPC", text, StringComparison.Ordinal);
        Assert.Contains("StructLDM", text, StringComparison.Ordinal);
        Assert.Contains("GaussCtrl", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TrAME", text, StringComparison.Ordinal);
        Assert.Contains("Kein Provider", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("S-Lab License", text, StringComparison.Ordinal);
        Assert.Contains("Kein Production-Button", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocalAiMeshEdit_IsNotImplemented_NoFakeButton()
    {
        var staticSvc = new FeatureAvailabilityService();
        Assert.Equal(FeatureAvailability.NotImplemented, staticSvc.GetStatus(FeatureIds.LocalAiMeshEdit));
        Assert.False(staticSvc.IsInvocable(FeatureIds.LocalAiMeshEdit));

        var dyn = new DynamicFeatureAvailabilityService();
        Assert.Equal(FeatureAvailability.NotImplemented, dyn.GetStatus(FeatureIds.LocalAiMeshEdit));
        Assert.False(dyn.IsInvocable(FeatureIds.LocalAiMeshEdit));
        Assert.Contains("NotImplemented", dyn.GetStatusMessage(FeatureIds.LocalAiMeshEdit), StringComparison.Ordinal);
        Assert.DoesNotContain("success", dyn.GetStatusMessage(FeatureIds.LocalAiMeshEdit), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No button", dyn.GetStatusMessage(FeatureIds.LocalAiMeshEdit), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductPath_RemainsParameterAndCatalogReplace()
    {
        Assert.Equal("valid", DeterministicAiParser.Parse("shoulders wider").Status);
        Assert.Equal("morph.local", DeterministicAiParser.Parse("shoulders wider").Operation);
        Assert.Equal("valid", DeterministicAiParser.Parse("rat head").Status);
        Assert.Equal("creature.replacePart", DeterministicAiParser.Parse("rat head").Operation);
        Assert.Equal(FeatureAvailability.Available, new DynamicFeatureAvailabilityService().GetStatus(FeatureIds.CreatureTextEdit));
    }

    [Fact]
    public void AiPanel_HasNoLocalMeshEditButton()
    {
        var xaml = File.ReadAllText(Path.Combine(RepoPaths.FindRepoRoot(), "3DGodCreator.App", "Panels", "AiPanel.xaml"));
        Assert.DoesNotContain("mesh.edit.ai", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("BlendedPC", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GaussCtrl", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StructLDM", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TrAME", xaml, StringComparison.Ordinal);
    }
}
