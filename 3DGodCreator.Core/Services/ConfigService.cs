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

    public Config Load()
    {
        if (!File.Exists(_configPath))
            return Config.Default;

        try
        {
            var json = File.ReadAllText(_configPath);
            var cfg = JsonSerializer.Deserialize<Config>(json);
            return cfg ?? Config.Default;
        }
        catch
        {
            return Config.Default;
        }
    }

    public void Save(Config config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }
}
