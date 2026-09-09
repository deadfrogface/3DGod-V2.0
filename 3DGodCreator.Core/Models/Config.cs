using System.Text.Json.Serialization;

namespace ThreeDGodCreator.Core.Models;

public class Config
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = "de";
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
    [JsonPropertyName("advanced_blender_fallback")]
    public bool AdvancedBlenderFallback { get; set; }
    [JsonPropertyName("gender")]
    public string Gender { get; set; } = "female";
    [JsonPropertyName("project_folder")]
    public string ProjectFolder { get; set; } = "";
    [JsonPropertyName("model_folder")]
    public string ModelFolder { get; set; } = "";
    [JsonPropertyName("cache_folder")]
    public string CacheFolder { get; set; } = "";
    [JsonPropertyName("export_folder")]
    public string ExportFolder { get; set; } = "";
    [JsonPropertyName("backend_quality")]
    public string BackendQuality { get; set; } = "balanced";
    [JsonPropertyName("prefer_gpu")]
    public bool PreferGpu { get; set; } = true;
    [JsonPropertyName("gpu_device")]
    public string GpuDevice { get; set; } = "auto";

    public static Config Default => new()
    {
        Language = "de",
        Theme = "dark",
        NsfwEnabled = true,
        ControllerEnabled = true,
        DebugEnabled = true,
        Gender = "male",
        BackendQuality = "balanced",
        PreferGpu = true,
        GpuDevice = "auto",
        AdvancedBlenderFallback = false
    };
}
