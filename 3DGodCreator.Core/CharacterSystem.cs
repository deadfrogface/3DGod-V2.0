using System.Collections.Generic;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core;

public class CharacterSystem
{
    private readonly ConfigService _configService;
    private readonly BlenderService _blenderService;
    private readonly PresetService _presetService;
    private readonly string _basePath;
    private System.Threading.Timer? _sculptDebounceTimer;
    private readonly object _sculptDebounceLock = new();

    public Dictionary<string, int> SculptData { get; } = new();
    public Dictionary<string, bool> AnatomyState { get; } = new();
    public Dictionary<string, List<string>> AssetState { get; } = new();
    public Dictionary<string, bool> PhysicsFlags { get; } = new();
    public Dictionary<string, MaterialData> Materials { get; } = new();

    public Config Config => _configService.Load();
    public bool NsfwEnabled { get; set; }

    public IViewport? Viewport { get; set; }
    public Action? SliderSyncCallback { get; set; }
    public Action? AnatomySyncCallback { get; set; }
    public bool IsCurrentModelRigged { get; set; }

    public CharacterSystem(ConfigService configService, BlenderService blenderService, PresetService presetService)
    {
        _configService = configService;
        _blenderService = blenderService;
        _presetService = presetService;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;

        InitializeSculptData();
        InitializeAnatomy();
        InitializeAssets();
        InitializePhysics();
        InitializeMaterials();

        var cfg = _configService.Load();
        NsfwEnabled = cfg.NsfwEnabled;

        if (!_presetService.Exists("default"))
            LoadBaseModel("male");
    }

    private void InitializeSculptData()
    {
        SculptData["height"] = 50;
        SculptData["breast_size"] = 50;
        SculptData["hip_width"] = 50;
        SculptData["arm_length"] = 50;
        SculptData["leg_length"] = 50;
        SculptData["thigh_size"] = 50;
        SculptData["forearm_size"] = 50;
        SculptData["symmetry"] = 1;
    }

    private void InitializeAnatomy()
    {
        AnatomyState["skin"] = true;
        AnatomyState["fat"] = true;
        AnatomyState["muscle"] = false;
        AnatomyState["bone"] = false;
        AnatomyState["organs"] = false;
        AnatomyState["breasts"] = true;
        AnatomyState["genitals"] = true;
        AnatomyState["bodyhair"] = false;
    }

    private void InitializeAssets()
    {
        AssetState["clothes"] = new List<string>();
        AssetState["piercings"] = new List<string>();
        AssetState["tattoos"] = new List<string>();
    }

    private void InitializePhysics()
    {
        PhysicsFlags["breasts"] = true;
        PhysicsFlags["cloth"] = true;
        PhysicsFlags["piercings"] = true;
    }

    private void InitializeMaterials()
    {
        Materials["skin"] = new MaterialData { Color = "#f5cba7", Roughness = 0.5, Metallic = 0 };
        Materials["clothes"] = new MaterialData { Color = "#cccccc", Roughness = 0.7, Metallic = 0 };
        Materials["piercings"] = new MaterialData { Color = "#aaaaaa", Roughness = 0.1, Metallic = 1 };
        Materials["tattoos"] = new MaterialData { Color = "#000000", Roughness = 0.9, Metallic = 0 };
    }

    public void SetGender(string gender)
    {
        var cfg = _configService.Load();
        cfg.Gender = gender;
        _configService.Save(cfg);
    }

    /// <summary>
    /// Update slider value. Viewport updates immediately (smooth). Blender JSON write is debounced.
    /// </summary>
    public void UpdateSculptValue(string key, int value)
    {
        SculptData[key] = value;
        RefreshLayers();
        DebouncedSendSculptData();
    }

    private void DebouncedSendSculptData()
    {
        lock (_sculptDebounceLock)
        {
            _sculptDebounceTimer?.Dispose();
            _sculptDebounceTimer = new System.Threading.Timer(_ =>
            {
                lock (_sculptDebounceLock)
                {
                    _sculptDebounceTimer?.Dispose();
                    _sculptDebounceTimer = null;
                }
                var dict = new Dictionary<string, object>();
                foreach (var kv in SculptData)
                    dict[kv.Key] = kv.Value;
                var charPath = Path.GetFullPath(Path.Combine(_basePath, GetCurrentModelPath()));
                if (File.Exists(charPath))
                    dict["_character_path"] = charPath;
                _blenderService.SendSculptData(dict);
            }, null, 150, System.Threading.Timeout.Infinite);
        }
    }

    public void Sculpt()
    {
        var dict = new Dictionary<string, object>();
        foreach (var kv in SculptData)
            dict[kv.Key] = kv.Value;
        var charPath = Path.GetFullPath(Path.Combine(_basePath, GetCurrentModelPath()));
        if (File.Exists(charPath))
            dict["_character_path"] = charPath;
        _blenderService.SendSculptData(dict);
        _blenderService.LaunchSculpt();
    }

    public void ExportFbx(string filename = "exported_character")
    {
        SavePreset(filename);
        _blenderService.ExportFbx(filename);
    }

    public void UpdateAnatomyLayer(string layerName, bool state)
    {
        AnatomyState[layerName] = state;
        RefreshLayers();
    }

    public void AddAsset(string category, string? path = null)
    {
        if (!AssetState.ContainsKey(category)) return;
        AssetState[category].Add(path ?? $"{category}_demo_asset");
        RefreshLayers();
    }

    public void RefreshLayers()
    {
        Viewport?.UpdatePreview(AnatomyState, AssetState);
        Viewport?.ApplySculptTransform(SculptData);
    }

    public void SavePreset(string name = "default")
    {
        _presetService.Save(name, new PresetData
        {
            SculptData = new Dictionary<string, int>(SculptData),
            Nsfw = NsfwEnabled,
            Anatomy = new Dictionary<string, bool>(AnatomyState),
            Assets = new Dictionary<string, List<string>>(AssetState.ToDictionary(kv => kv.Key, kv => new List<string>(kv.Value))),
            Physics = new Dictionary<string, bool>(PhysicsFlags),
            Materials = new Dictionary<string, MaterialData>(Materials)
        });
    }

    public void LoadPreset(string name = "default")
    {
        var data = _presetService.Load(name);
        if (data == null) return;

        foreach (var kv in data.SculptData)
            SculptData[kv.Key] = kv.Value;
        NsfwEnabled = data.Nsfw;
        foreach (var kv in data.Anatomy)
            AnatomyState[kv.Key] = kv.Value;
        foreach (var kv in data.Assets)
            AssetState[kv.Key] = new List<string>(kv.Value);
        foreach (var kv in data.Physics)
            PhysicsFlags[kv.Key] = kv.Value;
        foreach (var kv in data.Materials)
            Materials[kv.Key] = kv.Value;

        SliderSyncCallback?.Invoke();
        AnatomySyncCallback?.Invoke();
        Viewport?.UpdateView();
    }

    public void LoadBaseModel(string gender)
    {
        var relPath = Path.Combine("assets", "characters", $"{gender}_base.glb");
        var path = Path.GetFullPath(Path.Combine(_basePath, relPath));
        if (File.Exists(path))
        {
            Viewport?.LoadPreview(path);
        }
    }

    public string GetCurrentModelPath()
    {
        return Path.Combine("assets", "characters", $"{Config.Gender}_base.glb");
    }

    public void SetMaterialColor(string matKey, string hexColor)
    {
        if (Materials.TryGetValue(matKey, out var mat))
        {
            mat.Color = hexColor;
            RefreshLayers();
        }
    }

    public void CreateAutoRig()
    {
        _blenderService.LaunchAutoRig();
    }
}

public interface IViewport
{
    void LoadPreview(string path);
    void UpdateView();
    void UpdatePreview(Dictionary<string, bool> anatomy, Dictionary<string, List<string>> assets);
    void ApplySculptTransform(Dictionary<string, int> sculptData);
}
