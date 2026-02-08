using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ThreeDGodCreator.Core.Models;

public class PresetData
{
    [JsonPropertyName("sculpt_data")]
    public Dictionary<string, int> SculptData { get; set; } = new();
    [JsonPropertyName("nsfw")]
    public bool Nsfw { get; set; }
    [JsonPropertyName("anatomy")]
    public Dictionary<string, bool> Anatomy { get; set; } = new();
    [JsonPropertyName("assets")]
    public Dictionary<string, List<string>> Assets { get; set; } = new();
    [JsonPropertyName("physics")]
    public Dictionary<string, bool> Physics { get; set; } = new();
    [JsonPropertyName("materials")]
    public Dictionary<string, MaterialData> Materials { get; set; } = new();
}

public class MaterialData
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#cccccc";
    [JsonPropertyName("roughness")]
    public double Roughness { get; set; }
    [JsonPropertyName("metallic")]
    public double Metallic { get; set; }
    [JsonPropertyName("texture")]
    public string Texture { get; set; } = "";
}
