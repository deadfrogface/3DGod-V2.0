using System.Diagnostics;
using System.Text.Json;
using ThreeDGodCreator.Core.Models;

namespace ThreeDGodCreator.Core.Services;

public class BlenderService
{
    private readonly ConfigService _configService;
    private readonly string _basePath;

    public BlenderService(ConfigService configService)
    {
        _configService = configService;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
    }

    public string GetBlenderPath()
    {
        var config = _configService.Load();
        if (!string.IsNullOrEmpty(config.BlenderPath) && File.Exists(config.BlenderPath))
            return config.BlenderPath;

        var localApp = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\Blender Foundation");
        var progFiles = Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Blender Foundation");
        foreach (var baseDir in new[] { localApp, progFiles })
        {
            if (!Directory.Exists(baseDir)) continue;
            var dirs = Directory.GetDirectories(baseDir, "Blender *");
            if (dirs.Length == 0) continue;
            var exe = Path.Combine(dirs[^1], "blender.exe");
            if (File.Exists(exe))
                return exe;
        }
        return "blender";
    }

    public void SendSculptData(Dictionary<string, object> sculptData)
    {
        var inputPath = Path.Combine(_basePath, "blender_embed", "sculpt_input.json");
        Directory.CreateDirectory(Path.GetDirectoryName(inputPath)!);
        var json = JsonSerializer.Serialize(sculptData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(inputPath, json);
    }

    public void LaunchSculpt()
    {
        if (!EnsureBlender()) { Log("Blender nicht konfiguriert."); return; }
        var blenderPath = GetBlenderPath();

        var scriptPath = Path.GetFullPath(Path.Combine(_basePath, "blender_embed", "apply_sculpt_standalone.py"));
        if (!File.Exists(scriptPath))
        {
            scriptPath = Path.GetFullPath(Path.Combine(_basePath, "blender_embed", "scripts", "sculpt_apply.py"));
        }

        if (!File.Exists(scriptPath))
        {
            Log("Sculpt-Skript nicht gefunden");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = $"--background --python \"{scriptPath}\"",
                WorkingDirectory = _basePath,
                UseShellExecute = false
            };
            Process.Start(psi);
            Log("Blender Sculpting gestartet");
        }
        catch (Exception ex)
        {
            Log("Fehler beim Starten von Blender: " + ex.Message);
        }
    }

    public void LaunchAutoRig()
    {
        LaunchSculpt();
    }

    public void ExportFbx(string filename = "exported_character")
    {
        if (!EnsureBlender()) { Log("Blender nicht konfiguriert."); return; }
        var blenderPath = GetBlenderPath();
        var scriptPath = Path.GetFullPath(Path.Combine(_basePath, "blender_embed", "scripts", "export_fbx.py"));

        if (!File.Exists(scriptPath))
        {
            Log("export_fbx.py nicht gefunden");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = $"--background --python \"{scriptPath}\" -- {filename}",
                WorkingDirectory = _basePath,
                UseShellExecute = false
            };
            Process.Start(psi);
            Log("FBX Export gestartet");
        }
        catch (Exception ex)
        {
            Log("Fehler beim FBX Export: " + ex.Message);
        }
    }

    public event Action<string>? OnLog;
    public event Action? OnBlenderNotFound;

    public bool IsBlenderConfigured()
    {
        var p = GetBlenderPath();
        return !string.IsNullOrEmpty(p) && p != "blender" && File.Exists(p);
    }

    private void Log(string msg) => OnLog?.Invoke(msg);

    private bool EnsureBlender()
    {
        var p = GetBlenderPath();
        if (string.IsNullOrEmpty(p) || p == "blender" || !File.Exists(p))
        {
            OnBlenderNotFound?.Invoke();
            return false;
        }
        return true;
    }
}
