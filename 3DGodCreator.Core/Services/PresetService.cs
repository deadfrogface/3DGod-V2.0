using System.Text.Json;
using ThreeDGodCreator.Core.Models;

namespace ThreeDGodCreator.Core.Services;

public class PresetService
{
    private readonly string _presetPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public PresetService()
    {
        _presetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets");
    }

    public void Save(string name, PresetData data)
    {
        Directory.CreateDirectory(_presetPath);
        var path = Path.Combine(_presetPath, $"{name}.json");
        var dict = new Dictionary<string, object>
        {
            ["sculpt_data"] = data.SculptData,
            ["nsfw"] = data.Nsfw,
            ["anatomy"] = data.Anatomy,
            ["assets"] = data.Assets,
            ["physics"] = data.Physics,
            ["materials"] = data.Materials.ToDictionary(kv => kv.Key, kv => (object)new
            {
                color = kv.Value.Color,
                roughness = kv.Value.Roughness,
                metallic = kv.Value.Metallic,
                texture = kv.Value.Texture
            })
        };
        var json = JsonSerializer.Serialize(dict, JsonOptions);
        File.WriteAllText(path, json);
    }

    public PresetData? Load(string name)
    {
        var path = Path.Combine(_presetPath, $"{name}.json");
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PresetData>(json);
        }
        catch
        {
            return null;
        }
    }

    public bool Exists(string name)
    {
        return File.Exists(Path.Combine(_presetPath, $"{name}.json"));
    }
}
