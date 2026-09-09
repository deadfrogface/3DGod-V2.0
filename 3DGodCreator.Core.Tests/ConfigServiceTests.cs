using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class ConfigServiceTests
{
    [Fact]
    public void SaveAndLoad_RoundtripsConfigValues()
    {
        var path = Path.Combine(Path.GetTempPath(), "3dgod-cfg-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var service = new ConfigService(path);
            var original = new Config
            {
                Language = "en",
                Theme = "cyberpunk",
                NsfwEnabled = false,
                ControllerEnabled = false,
                DebugEnabled = true,
                BlenderPath = "",
                Gender = "male",
                BackendQuality = "quality",
                PreferGpu = false,
                AdvancedBlenderFallback = true,
                ProjectFolder = Path.Combine(Path.GetTempPath(), "3dgod-proj-" + Guid.NewGuid().ToString("N")),
                ModelFolder = Path.Combine(Path.GetTempPath(), "3dgod-models-" + Guid.NewGuid().ToString("N")),
                CacheFolder = Path.Combine(Path.GetTempPath(), "3dgod-cache-" + Guid.NewGuid().ToString("N")),
                ExportFolder = Path.Combine(Path.GetTempPath(), "3dgod-export-" + Guid.NewGuid().ToString("N"))
            };

            service.Save(original);
            var loaded = service.Load();

            Assert.Equal(original.Language, loaded.Language);
            Assert.Equal(original.Theme, loaded.Theme);
            Assert.Equal(original.NsfwEnabled, loaded.NsfwEnabled);
            Assert.Equal(original.ControllerEnabled, loaded.ControllerEnabled);
            Assert.Equal(original.DebugEnabled, loaded.DebugEnabled);
            Assert.Equal(original.Gender, loaded.Gender);
            Assert.Equal("quality", loaded.BackendQuality);
            Assert.False(loaded.PreferGpu);
            Assert.True(loaded.AdvancedBlenderFallback);
            Assert.True(Directory.Exists(loaded.ProjectFolder));
            Assert.True(Directory.Exists(loaded.ModelFolder));
            Assert.True(Directory.Exists(loaded.CacheFolder));
            Assert.True(Directory.Exists(loaded.ExportFolder));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Sanitize_RecoversInvalidPaths_AndMissingBlender()
    {
        var cfg = new Config
        {
            ProjectFolder = "?:\\not-a-valid-path\0",
            ModelFolder = "",
            CacheFolder = "",
            ExportFolder = "",
            BlenderPath = Path.Combine(Path.GetTempPath(), "missing-blender-" + Guid.NewGuid().ToString("N") + ".exe"),
            Language = "fr",
            Theme = "neon",
            BackendQuality = "ultra"
        };

        var clean = ConfigService.Sanitize(cfg);
        Assert.Equal("de", clean.Language);
        Assert.Equal("dark", clean.Theme);
        Assert.Equal("balanced", clean.BackendQuality);
        Assert.True(Directory.Exists(clean.ProjectFolder));
        Assert.True(Directory.Exists(clean.ModelFolder));
        Assert.True(Directory.Exists(clean.CacheFolder));
        Assert.True(Directory.Exists(clean.ExportFolder));
        Assert.Equal("", clean.BlenderPath);
    }

    [Fact]
    public void Default_HasExpectedBaselineValues()
    {
        var cfg = Config.Default;
        Assert.Equal("de", cfg.Language);
        Assert.Equal("dark", cfg.Theme);
        Assert.Equal("male", cfg.Gender);
        Assert.Equal("balanced", cfg.BackendQuality);
        Assert.True(cfg.NsfwEnabled);
        Assert.True(cfg.ControllerEnabled);
        Assert.True(cfg.DebugEnabled);
        Assert.True(cfg.PreferGpu);
    }
}
