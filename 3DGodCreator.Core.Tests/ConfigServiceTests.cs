using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class ConfigServiceTests
{
    [Fact]
    public void SaveAndLoad_RoundtripsConfigValues()
    {
        var service = new ConfigService();
        var original = new Config
        {
            Theme = "cyberpunk",
            NsfwEnabled = false,
            ControllerEnabled = false,
            DebugEnabled = true,
            BlenderPath = @"C:\Program Files\Blender Foundation\Blender 4.0\blender.exe",
            Gender = "male"
        };

        service.Save(original);
        var loaded = service.Load();

        Assert.Equal(original.Theme, loaded.Theme);
        Assert.Equal(original.NsfwEnabled, loaded.NsfwEnabled);
        Assert.Equal(original.ControllerEnabled, loaded.ControllerEnabled);
        Assert.Equal(original.DebugEnabled, loaded.DebugEnabled);
        Assert.Equal(original.BlenderPath, loaded.BlenderPath);
        Assert.Equal(original.Gender, loaded.Gender);
    }

    [Fact]
    public void Default_HasExpectedBaselineValues()
    {
        var cfg = Config.Default;
        Assert.Equal("dark", cfg.Theme);
        Assert.Equal("male", cfg.Gender);
        Assert.True(cfg.NsfwEnabled);
        Assert.True(cfg.ControllerEnabled);
        Assert.True(cfg.DebugEnabled);
    }
}
