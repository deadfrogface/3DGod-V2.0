using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class PresetServiceTests
{
    [Fact]
    public void SaveLoadExists_RoundtripsPresetData()
    {
        var service = new PresetService();
        var name = "phase00_smoke_" + Guid.NewGuid().ToString("N");
        var data = new PresetData
        {
            Nsfw = true,
            SculptData = new Dictionary<string, int> { ["height"] = 72, ["hip_width"] = 40 },
            Anatomy = new Dictionary<string, bool> { ["skin"] = true, ["muscle"] = false },
            Assets = new Dictionary<string, List<string>> { ["clothes"] = new() { "jacket_a" } },
            Physics = new Dictionary<string, bool> { ["cloth"] = false },
            Materials = new Dictionary<string, MaterialData>
            {
                ["skin"] = new() { Color = "#f5cba7", Roughness = 0.5, Metallic = 0, Texture = "" }
            }
        };

        try
        {
            Assert.False(service.Exists(name));
            service.Save(name, data);
            Assert.True(service.Exists(name));

            var loaded = service.Load(name);
            Assert.NotNull(loaded);
            Assert.True(loaded!.Nsfw);
            Assert.Equal(72, loaded.SculptData["height"]);
            Assert.Equal(40, loaded.SculptData["hip_width"]);
            Assert.True(loaded.Anatomy["skin"]);
            Assert.False(loaded.Anatomy["muscle"]);
            Assert.Equal("jacket_a", loaded.Assets["clothes"][0]);
            Assert.False(loaded.Physics["cloth"]);
            Assert.Equal("#f5cba7", loaded.Materials["skin"].Color);
            Assert.Equal(0.5, loaded.Materials["skin"].Roughness);
            Assert.Equal(0, loaded.Materials["skin"].Metallic);
        }
        finally
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets", name + ".json");
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Load_MissingPreset_ReturnsNull()
    {
        var service = new PresetService();
        Assert.Null(service.Load("does_not_exist_" + Guid.NewGuid().ToString("N")));
    }
}
