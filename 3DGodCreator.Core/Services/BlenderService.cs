using System.Diagnostics;
using System.Text.Json;
using ThreeDGodCreator.Core.Models;

namespace ThreeDGodCreator.Core.Services;

public class BlenderService
{
    private readonly ConfigService _configService;
    private readonly string _basePath;
    private Process? _lastBlenderProcess;

    public BlenderService(ConfigService configService)
    {
        _configService = configService;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>
    /// Gets Blender executable path: config first, then auto-detect.
    /// </summary>
    public string GetBlenderPath()
    {
        var config = _configService.Load();
        if (!string.IsNullOrEmpty(config.BlenderPath) && File.Exists(config.BlenderPath))
            return config.BlenderPath;
        return DetectBlenderPath() ?? "blender";
    }

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

    public bool VerifyCanLaunch(out string? errorMessage) =>
        ProjectReadinessService.VerifyBlenderCanLaunch(GetBlenderPath(), out errorMessage);

    public void SendSculptData(Dictionary<string, object> sculptData)
    {
        try
        {
            var inputPath = Path.Combine(_basePath, "blender_embed", "sculpt_input.json");
            Directory.CreateDirectory(Path.GetDirectoryName(inputPath)!);
            var json = JsonSerializer.Serialize(sculptData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(inputPath, json);
            AppLogger.Write($"[Blender] Sculpt data written to {inputPath}");
        }
        catch (Exception ex)
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.Unknown, "Failed to write sculpt data", ex.Message,
                "Check write permissions for blender_embed folder.");
            ReportBlenderError(err);
            AppLogger.LogException(ex, "SendSculptData");
        }
    }

    /// <summary>
    /// Launch Blender with sculpt script. GUI mode so Blender STAYS OPEN.
    /// </summary>
    public void LaunchSculpt()
    {
        var path = GetBlenderPath();
        if (string.IsNullOrEmpty(path) || path == "blender" || !File.Exists(path))
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.NotInstalled, "Blender executable not found",
                $"Path: {path}", "Install Blender or set path in Settings (Einstellungen).");
            ReportBlenderError(err);
            OnBlenderNotFound?.Invoke();
            return;
        }

        var scriptPath = Path.GetFullPath(Path.Combine(_basePath, "blender_embed", "apply_sculpt_standalone.py"));
        if (!File.Exists(scriptPath))
            scriptPath = Path.GetFullPath(Path.Combine(_basePath, "blender_embed", "scripts", "sculpt_apply.py"));

        if (!File.Exists(scriptPath))
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.ScriptNotFound, "Sculpt script not found",
                $"Expected: {scriptPath}", "Run 'dotnet build' to copy blender_embed to output.");
            ReportBlenderError(err);
            Log("Sculpt-Skript nicht gefunden");
            return;
        }

        AppLogger.Write($"[Blender] Script path: {scriptPath}");
        LaunchBlenderProcess(path, $"--python \"{scriptPath}\"", "Sculpt", keepAlive: true);
    }

    public void LaunchAutoRig() => LaunchSculpt();

    public void ExportFbx(string filename = "exported_character")
    {
        var path = GetBlenderPath();
        if (string.IsNullOrEmpty(path) || path == "blender" || !File.Exists(path))
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.NotInstalled, "Blender executable not found", null,
                "Set Blender path in Settings.");
            ReportBlenderError(err);
            OnBlenderNotFound?.Invoke();
            return;
        }

        var scriptPath = Path.GetFullPath(Path.Combine(_basePath, "blender_embed", "scripts", "export_fbx.py"));
        if (!File.Exists(scriptPath))
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.ScriptNotFound, "export_fbx.py not found", scriptPath,
                "Run 'dotnet build'.");
            ReportBlenderError(err);
            Log("export_fbx.py nicht gefunden");
            return;
        }

        LaunchBlenderProcess(path, $"--background --python \"{scriptPath}\" -- {filename}", "FBX Export", keepAlive: false);
    }

    /// <summary>
    /// Launch Blender process. keepAlive=true = GUI mode (no --background).
    /// </summary>
    private void LaunchBlenderProcess(string blenderPath, string arguments, string operation, bool keepAlive = false)
    {
        AppLogger.Write($"[Blender] Launching: {blenderPath} {arguments} (keepAlive={keepAlive})");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = arguments,
                WorkingDirectory = _basePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var startTime = DateTime.UtcNow;

            proc.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppLogger.Write($"[Blender stdout] {e.Data}");
                    Log(e.Data);
                }
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppLogger.Write($"[Blender stderr] {e.Data}", isError: true);
                    Log($"[stderr] {e.Data}");
                }
            };

            proc.Exited += (_, _) =>
            {
                _lastBlenderProcess = null;
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

                AppLogger.Write($"[Blender] Process exited. Code={proc.ExitCode}, Elapsed={elapsed:F1}s");

                var isSuspicious = keepAlive && elapsed < 5;
                var isError = proc.ExitCode != 0 && proc.ExitCode != -1;

                if (isSuspicious || isError)
                {
                    var reason = isSuspicious
                        ? $"Blender quit after {elapsed:F1}s. Sculpt mode should keep Blender OPEN."
                        : $"Process exited with code {proc.ExitCode}";
                    var err = new BlenderErrorInfo(BlenderErrorCode.ProcessExitedUnexpectedly,
                        reason, "Check error_log.txt for stdout/stderr and Python traceback.",
                        "Verify sculpt_input.json exists. Script path logged at launch.");
                    ReportBlenderError(err);
                }
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            _lastBlenderProcess = proc;

            Log($"{operation} gestartet (PID {proc.Id})" + (keepAlive ? " - Blender bleibt offen" : ""));
            AppLogger.Write($"[Blender] {operation} started PID={proc.Id}");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.PermissionDenied,
                "Could not start Blender process", ex.Message,
                "Check path and permissions. Try running as administrator.");
            ReportBlenderError(err);
        }
        catch (Exception ex)
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.ProcessStartFailed, ex.Message, ex.StackTrace,
                "Verify Blender path in Settings.");
            ReportBlenderError(err);
            AppLogger.LogException(ex, "LaunchBlenderProcess");
        }
    }

    private void ReportBlenderError(BlenderErrorInfo info)
    {
        var msg = $"{info.Code}: {info.Message}";
        if (!string.IsNullOrEmpty(info.Detail)) msg += $" | {info.Detail}";
        if (!string.IsNullOrEmpty(info.SuggestedFix)) msg += $" -> {info.SuggestedFix}";
        AppLogger.Write($"[Blender] {msg}", isError: true);
        Log(msg);
        OnBlenderFailed?.Invoke(info);
    }

    public event Action<string>? OnLog;
    public event Action? OnBlenderNotFound;
    public event Action<BlenderErrorInfo>? OnBlenderFailed;

    public bool IsBlenderConfigured()
    {
        var p = GetBlenderPath();
        return !string.IsNullOrEmpty(p) && p != "blender" && File.Exists(p);
    }

    public bool IsBlenderProcessRunning => _lastBlenderProcess != null && !_lastBlenderProcess.HasExited;

    private void Log(string msg)
    {
        OnLog?.Invoke(msg);
    }

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
