using ThreeDGod.Infrastructure.Components;

namespace ThreeDGodCreator.Core.Tests;

public class SetupAssistantCatalogTests
{
    [Fact]
    public void Snapshot_ExposesProductFeatures_WithoutRequiringBackends()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-setup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mgr = new ComponentManager(root, []);
            var snap = SetupAssistantCatalog.Snapshot(mgr);
            Assert.Contains(snap, s => s.Feature.FeatureId == SetupFeatureId.HumanCreator);
            Assert.Contains(snap, s => s.Feature.FeatureId == SetupFeatureId.ParametricClothing);
            Assert.All(snap, s => Assert.True(s.Feature.Optional));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
