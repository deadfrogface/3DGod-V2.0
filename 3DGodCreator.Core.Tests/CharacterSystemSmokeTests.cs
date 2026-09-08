using ThreeDGod.Infrastructure;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class CharacterSystemSmokeTests
{
    [Fact]
    public void Constructor_InitializesInMemoryState()
    {
        var cs = new CharacterSystem(new ConfigService(), new LegacyBlenderBackend(new ConfigService()), new PresetService());

        Assert.Equal(50, cs.SculptData["height"]);
        Assert.True(cs.AnatomyState["skin"]);
        Assert.Empty(cs.AssetState["clothes"]);
        Assert.True(cs.PhysicsFlags["breasts"]);
        Assert.Equal("#f5cba7", cs.Materials["skin"].Color);
    }

    [Fact]
    public void AddAsset_AppendsPlaceholderString_NotAMesh()
    {
        var cs = new CharacterSystem(new ConfigService(), new LegacyBlenderBackend(new ConfigService()), new PresetService());
        cs.AddAsset("clothes");
        Assert.Single(cs.AssetState["clothes"]);
        Assert.Equal("clothes_demo_asset", cs.AssetState["clothes"][0]);
    }

    [Fact]
    public void GetCurrentModelPath_UsesConfiguredGender()
    {
        var cs = new CharacterSystem(new ConfigService(), new LegacyBlenderBackend(new ConfigService()), new PresetService());
        var path = cs.GetCurrentModelPath();
        Assert.Contains("assets", path, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("_base.glb", path, StringComparison.OrdinalIgnoreCase);
    }
}
