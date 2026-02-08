using System.Text.Json;
using ThreeDGodCreator.Core.Models;

namespace ThreeDGodCreator.Core.Services;

public class ConfigService
{
    private const string ConfigPath = "config.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public Config Load()
    {
        if (!File.Exists(ConfigPath))
            return Config.Default;

        try
        {
            var json = File.ReadAllText(ConfigPath);
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
        File.WriteAllText(ConfigPath, json);
    }
}
