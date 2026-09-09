using System.Diagnostics;
using System.Text.Json;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGod.Infrastructure;

/// <summary>
/// Headless legacy Blender process host. Never opens a Blender UI window.
/// </summary>
public class LegacyBlenderBackend : IBlenderOperations
{
    private readonly ConfigService _configService;
    private readonly string _basePath;
    private Process? _lastBlenderProcess;

    public LegacyBlenderBackend(ConfigService configService)
    {
        _configService = configService;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
    }

    public string GetBlenderPath()
    {
        var config = _configService.Load();
        if (!string.IsNullOrEmpty(config.BlenderPath))
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
            AppLogger.Write($"[LegacyRuntime] Sculpt data written to {inputPath}");
        }
        catch (Exception ex)
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.Unknown, "Failed to write sculpt data", ex.Message,
                "Check write permissions for blender_embed folder.");
            ReportBlenderError(err);
            AppLogger.LogException(ex, "SendSculptData");
        }
    }

    public void LaunchSculpt()
    {
        var path = GetBlenderPath();
        if (string.IsNullOrEmpty(path) || path == "blender" || !File.Exists(path))
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.NotInstalled, "External mesh tool not found",
                $"Path: {path}", "Set optional runtime path in Settings.");
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

        LaunchHeadlessProcess(path, $"--background --python \"{scriptPath}\"", "Sculpt");
    }

    public void LaunchAutoRig()
    {
        // Honest gate: legacy Blender must not pretend Auto-Rig by opening Sculpt.
        var err = new BlenderErrorInfo(
            BlenderErrorCode.Unknown,
            "NotImplemented – Auto-Rig is not available via the legacy Blender host. No skeleton will be generated and Sculpt will not be started.",
            "Use SkinTokens/Anny rig paths when Available/Experimental; do not map AutoRig→Sculpt.",
            "Leave Auto-Rig disabled until a real auto-rig backend is wired.");
        ReportBlenderError(err);
        Log("Auto-Rig: NotImplemented (will not launch Sculpt)");
    }

    public void ExportFbx(string filename = "exported_character")
    {
        var path = GetBlenderPath();
        if (string.IsNullOrEmpty(path) || path == "blender" || !File.Exists(path))
        {
            var err = new BlenderErrorInfo(BlenderErrorCode.NotInstalled, "External mesh tool not found", null,
                "Set optional runtime path in Settings.");
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

        LaunchHeadlessProcess(path, $"--background --python \"{scriptPath}\" -- {filename}", "FBX Export");
    }

    public bool TryExportGlbToFbx(string sourceGlb, string destinationFbx, out string? error)
    {
        error = null;
        var blenderPath = GetBlenderPath();
        if (string.IsNullOrEmpty(blenderPath) || blenderPath == "blender" || !File.Exists(blenderPath))
        {
            error = "GATED_NOT_INSTALLED – Blender runtime missing; FBX export not executed.";
            return false;
        }

        if (!File.Exists(sourceGlb))
        {
            error = "GLB source missing: " + sourceGlb;
            return false;
        }

        var scriptPath = ResolveBlenderScript("export_fbx.py");
        if (scriptPath is null)
        {
            error = "export_fbx.py not found under blender_embed/scripts (BaseDirectory or repo root).";
            return false;
        }

        var src = Path.GetFullPath(sourceGlb);
        var dst = Path.GetFullPath(destinationFbx);
        var ok = TryRunHeadlessJob(
            scriptPath,
            [src, dst],
            out var output,
            out error,
            timeoutMs: 120_000);
        if (!ok)
            return false;

        if (!output.Contains("FBX_EXPORT_OK", StringComparison.Ordinal))
        {
            error = string.IsNullOrWhiteSpace(error)
                ? "Blender FBX job finished without FBX_EXPORT_OK marker."
                : error;
            return false;
        }

        if (!File.Exists(dst))
        {
            error = "FBX not written: " + dst;
            return false;
        }

        if (new FileInfo(dst).Length < ThreeDGod.Export.FbxSanity.MinimumBytes)
        {
            error = "FBX too small after export.";
            return false;
        }

        return true;
    }

    public bool TryRunHeadlessJob(string pythonScriptPath, out string output, out string? error) =>
        TryRunHeadlessJob(pythonScriptPath, null, out output, out error);

    public bool TryRunHeadlessJob(
        string pythonScriptPath,
        IReadOnlyList<string>? scriptArgs,
        out string output,
        out string? error,
        int timeoutMs = 20000)
    {
        output = "";
        error = null;
        var blenderPath = GetBlenderPath();
        if (string.IsNullOrEmpty(blenderPath) || blenderPath == "blender" || !File.Exists(blenderPath))
        {
            error = "Legacy runtime unavailable (not installed).";
            return false;
        }

        if (!File.Exists(pythonScriptPath))
        {
            error = "Script not found: " + pythonScriptPath;
            return false;
        }

        try
        {
            var argTail = scriptArgs is { Count: > 0 }
                ? " -- " + string.Join(" ", scriptArgs.Select(a => $"\"{a}\""))
                : "";
            var psi = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = $"--background --python \"{pythonScriptPath}\"{argTail}",
                WorkingDirectory = _basePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null)
            {
                error = "Process.Start returned null.";
                return false;
            }

            output = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            if (!proc.WaitForExit(timeoutMs))
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* best-effort */ }
                error = "Headless job timed out.";
                return false;
            }

            if (proc.ExitCode != 0 && proc.ExitCode != -1)
            {
                error = string.IsNullOrWhiteSpace(stderr) ? $"Exit code {proc.ExitCode}" : stderr.Trim();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            AppLogger.LogException(ex, "TryRunHeadlessJob");
            return false;
        }
    }

    private void LaunchHeadlessProcess(string blenderPath, string arguments, string operation)
    {
        var args = arguments.Contains("--background", StringComparison.Ordinal)
            ? arguments
            : "--background " + arguments;
        AppLogger.Write($"[LegacyRuntime] Launching headless: {blenderPath} {args}");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = args,
                WorkingDirectory = _basePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

            proc.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppLogger.Write($"[LegacyRuntime stdout] {e.Data}");
                    Log(e.Data);
                }
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppLogger.Write($"[LegacyRuntime stderr] {e.Data}", isError: true);
                    Log($"[stderr] {e.Data}");
                }
            };

            proc.Exited += (_, _) =>
            {
                _lastBlenderProcess = null;
                AppLogger.Write($"[LegacyRuntime] Process exited. Code={proc.ExitCode}");
                if (proc.ExitCode != 0 && proc.ExitCode != -1)
                {
                    ReportBlenderError(new BlenderErrorInfo(BlenderErrorCode.ProcessExitedUnexpectedly,
                        $"Process exited with code {proc.ExitCode}",
                        "Check error_log.txt.",
                        "Verify scripts exist and the runtime path is valid."));
                }
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            _lastBlenderProcess = proc;
            Log($"{operation} headless gestartet (PID {proc.Id})");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            ReportBlenderError(new BlenderErrorInfo(BlenderErrorCode.PermissionDenied,
                "Could not start legacy runtime process", ex.Message, "Check path and permissions."));
        }
        catch (Exception ex)
        {
            ReportBlenderError(new BlenderErrorInfo(BlenderErrorCode.ProcessStartFailed, ex.Message, ex.StackTrace,
                "Verify optional runtime path in Settings."));
            AppLogger.LogException(ex, "LaunchHeadlessProcess");
        }
    }

    private void ReportBlenderError(BlenderErrorInfo info)
    {
        var msg = $"{info.Code}: {info.Message}";
        if (!string.IsNullOrEmpty(info.Detail)) msg += $" | {info.Detail}";
        if (!string.IsNullOrEmpty(info.SuggestedFix)) msg += $" -> {info.SuggestedFix}";
        AppLogger.Write($"[LegacyRuntime] {msg}", isError: true);
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

    private string? ResolveBlenderScript(string fileName)
    {
        var candidates = new List<string>
        {
            Path.Combine(_basePath, "blender_embed", "scripts", fileName),
            Path.Combine(_basePath, "blender_embed", fileName)
        };

        for (var dir = new DirectoryInfo(_basePath); dir is not null; dir = dir.Parent)
        {
            candidates.Add(Path.Combine(dir.FullName, "blender_embed", "scripts", fileName));
            candidates.Add(Path.Combine(dir.FullName, "blender_embed", fileName));
            if (File.Exists(Path.Combine(dir.FullName, "3DGodCreator.sln")))
                break;
        }

        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    private void Log(string msg) => OnLog?.Invoke(msg);
}
