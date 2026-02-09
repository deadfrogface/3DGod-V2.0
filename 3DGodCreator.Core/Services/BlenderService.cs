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

    /// <summary>
    /// Gets Blender executable path: config first, then auto-detect (common paths + PATH).
    /// Returns "blender" if not found (fallback for PATH - may fail on Windows).
    /// </summary>
    public string GetBlenderPath()
    {
        var config = _configService.Load();
        if (!string.IsNullOrEmpty(config.BlenderPath) && File.Exists(config.BlenderPath))
            return config.BlenderPath;

        var found = DetectBlenderPath();
        return found ?? "blender";
    }

    /// <summary>
    /// Tries to detect Blender in common install locations and PATH.
    /// </summary>
    public string? DetectBlenderPath()
    {
        var searchBases = new[]
        {
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\Blender Foundation"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Blender Foundation"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Blender Foundation")
        };
        foreach (var baseDir in searchBases)
        {
            if (!Directory.Exists(baseDir)) continue;
            var dirs = Directory.GetDirectories(baseDir, "Blender *");
            if (dirs.Length == 0) continue;
            var sorted = dirs.OrderByDescending(d => d).ToArray();
            var exe = Path.Combine(sorted[0], "blender.exe");
            if (File.Exists(exe))
                return exe;
        }
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var exe = Path.Combine(dir.Trim(), "blender.exe");
            if (File.Exists(exe))
                return exe;
        }
        return null;
    }

    /// <summary>
    /// Verifies that Blender can be launched. Returns true if successful.
    /// </summary>
    public bool VerifyCanLaunch(out string? errorMessage)
    {
        var path = GetBlenderPath();
        if (string.IsNullOrEmpty(path) || path == "blender" || !File.Exists(path))
        {
            errorMessage = "Blender executable not found. Set path in Settings.";
            return false;
        }
        return ProjectReadinessService.VerifyBlenderCanLaunch(path, out errorMessage);
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
