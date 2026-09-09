using System.Text.Json;
using ThreeDGodCreator.Core.Models;

namespace ThreeDGodCreator.Core.Services;

public class ConfigService
{
    private readonly string _configPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ConfigService()
    {
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    }

    /// <summary>Test/alternate path constructor.</summary>
    public ConfigService(string configPath) => _configPath = configPath;

    public Config Load()
    {
        if (!File.Exists(_configPath))
            return Sanitize(Config.Default);

        try
        {
            var json = File.ReadAllText(_configPath);
            var cfg = JsonSerializer.Deserialize<Config>(json);
            return Sanitize(cfg ?? Config.Default);
        }
        catch
        {
            return Sanitize(Config.Default);
        }
    }

    public void Save(Config config)
    {
        var clean = Sanitize(config);
        var json = JsonSerializer.Serialize(clean, JsonOptions);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_configPath))!);
        File.WriteAllText(_configPath, json);
    }

    /// <summary>
    /// Recovers invalid folder/file paths to safe defaults under LocalApplicationData/3DGod.
    /// Empty paths become defaults; non-existing folders are created when possible.
    /// </summary>
    public static Config Sanitize(Config config)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod");

        config.Language = NormalizeLanguage(config.Language);
        config.Theme = NormalizeTheme(config.Theme);
        config.BackendQuality = NormalizeQuality(config.BackendQuality);
        config.GpuDevice = string.IsNullOrWhiteSpace(config.GpuDevice) ? "auto" : config.GpuDevice.Trim();

        config.ProjectFolder = RecoverFolder(config.ProjectFolder, Path.Combine(root, "Projects"));
        config.ModelFolder = RecoverFolder(config.ModelFolder, Path.Combine(root, "Models"));
        config.CacheFolder = RecoverFolder(config.CacheFolder, Path.Combine(root, "Cache"));
        config.ExportFolder = RecoverFolder(config.ExportFolder, Path.Combine(root, "Exports"));

        if (!string.IsNullOrWhiteSpace(config.BlenderPath) && !File.Exists(config.BlenderPath))
            config.BlenderPath = "";

        return config;
    }

    private static string RecoverFolder(string? configured, string fallback)
    {
        var path = string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
        try
        {
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                path = fallback;
            Directory.CreateDirectory(path);
            return Path.GetFullPath(path);
        }
        catch
        {
            try
            {
                Directory.CreateDirectory(fallback);
                return Path.GetFullPath(fallback);
            }
            catch
            {
                return fallback;
            }
        }
    }

    private static string NormalizeLanguage(string? language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "de";

    private static string NormalizeTheme(string? theme) => theme?.ToLowerInvariant() switch
    {
        "light" => "light",
        "cyberpunk" => "cyberpunk",
        _ => "dark"
    };

    private static string NormalizeQuality(string? quality) => quality?.ToLowerInvariant() switch
    {
        "draft" or "fast" => "draft",
        "quality" or "high" => "quality",
        _ => "balanced"
    };
}
