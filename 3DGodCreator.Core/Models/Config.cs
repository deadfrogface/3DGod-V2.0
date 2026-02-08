using System.Text.Json.Serialization;

namespace ThreeDGodCreator.Core.Models;

public class Config
{
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "dark";
    [JsonPropertyName("nsfw_enabled")]
    public bool NsfwEnabled { get; set; } = true;
    [JsonPropertyName("controller_enabled")]
    public bool ControllerEnabled { get; set; } = true;
    [JsonPropertyName("debug_enabled")]
    public bool DebugEnabled { get; set; } = true;
    [JsonPropertyName("blender_path")]
    public string BlenderPath { get; set; } = "";
    [JsonPropertyName("gender")]
    public string Gender { get; set; } = "female";

    public static Config Default => new()
    {
        Theme = "dark",
        NsfwEnabled = true,
        ControllerEnabled = true,
        DebugEnabled = true,
        Gender = "male"
    };
}
