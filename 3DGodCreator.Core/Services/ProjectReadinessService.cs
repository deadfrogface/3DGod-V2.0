using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ThreeDGodCreator.Core.Services;

/// <summary>
/// Result of a single readiness check.
/// </summary>
public record CheckResult(bool Success, string Message, string? SuggestedFix = null);

/// <summary>
/// Full project readiness check result.
/// </summary>
public record ReadinessResult(
    bool AllCriticalPassed,
    CheckResult Blender,
    CheckResult DotNet,
    CheckResult NuGetPackages,
    CheckResult Assets,
    CheckResult BlenderScripts,
    CheckResult Permissions,
    IReadOnlyList<string> SummaryLines
);

/// <summary>
/// Verifies that the entire project is ready to run.
/// Checks .NET, NuGet, assets, Blender, scripts, and permissions.
/// </summary>
public static class ProjectReadinessService
{
    private static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly ConfigService ConfigService = new();

    /// <summary>
    /// Run all readiness checks. Results are logged via StartupLogger.
    /// </summary>
    public static ReadinessResult RunFullCheck()
    {
        StartupLogger.Initialize();
        var step = 1;
        var summary = new List<string>();

        // --- Step 1: .NET SDK ---
        var dotNet = CheckDotNet();
        if (dotNet.Success)
            StartupLogger.StepOk(step, ".NET SDK", dotNet.Message);
        else
            StartupLogger.StepFail(step, ".NET SDK", dotNet.Message, dotNet.SuggestedFix);
        summary.Add(FormatSummary(step, ".NET SDK", dotNet.Success));
        step++;

        // --- Step 2: NuGet packages ---
        var nuget = CheckNuGetPackages();
        if (nuget.Success)
            StartupLogger.StepOk(step, "NuGet packages", nuget.Message);
        else
            StartupLogger.StepFail(step, "NuGet packages", nuget.Message, nuget.SuggestedFix);
        summary.Add(FormatSummary(step, "NuGet packages", nuget.Success));
        step++;

        // --- Step 3: Blender ---
        var blender = CheckBlender();
        if (blender.Success)
            StartupLogger.StepOk(step, "Blender", blender.Message);
        else
            StartupLogger.StepFail(step, "Blender", blender.Message, blender.SuggestedFix);
        summary.Add(FormatSummary(step, "Blender", blender.Success));
        step++;

        // --- Step 4: Assets ---
        var assets = CheckAssets();
        if (assets.Success)
            StartupLogger.StepOk(step, "Project assets", assets.Message);
        else
            StartupLogger.StepFail(step, "Project assets", assets.Message, assets.SuggestedFix);
        summary.Add(FormatSummary(step, "Project assets", assets.Success));
        step++;

        // --- Step 5: Blender scripts ---
        var scripts = CheckBlenderScripts();
        if (scripts.Success)
            StartupLogger.StepOk(step, "Blender scripts", scripts.Message);
        else
            StartupLogger.StepFail(step, "Blender scripts", scripts.Message, scripts.SuggestedFix);
        summary.Add(FormatSummary(step, "Blender scripts", scripts.Success));
        step++;

        // --- Step 6: Permissions ---
        var perms = CheckPermissions();
        if (perms.Success)
            StartupLogger.StepOk(step, "Access permissions", perms.Message);
        else
            StartupLogger.StepWarn(step, "Access permissions", perms.Message);
        summary.Add(FormatSummary(step, "Access permissions", perms.Success));

        StartupLogger.Write($"Log file: {StartupLogger.GetLogFilePath()}");

        var allCritical = dotNet.Success && nuget.Success && assets.Success && scripts.Success;
        return new ReadinessResult(
            AllCriticalPassed: allCritical,
            Blender: blender,
            DotNet: dotNet,
            NuGetPackages: nuget,
            Assets: assets,
            BlenderScripts: scripts,
            Permissions: perms,
            SummaryLines: summary
        );
    }

    private static string FormatSummary(int step, string name, bool ok) =>
        $"Step {step}: {name} {(ok ? "[OK]" : "[FAIL]")}";

    /// <summary>
    /// Check if .NET SDK (net8.0) is available.
    /// </summary>
    public static CheckResult CheckDotNet()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null)
                return new CheckResult(false, "Could not start dotnet process",
                    "Install .NET 8 SDK: https://dotnet.microsoft.com/download");

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);
            if (proc.ExitCode != 0)
                return new CheckResult(false, $"dotnet --version failed (exit {proc.ExitCode})",
                    "Install .NET 8 SDK: https://dotnet.microsoft.com/download");

            var version = output.Trim();
            if (version.StartsWith("8.") || version.StartsWith("9."))
                return new CheckResult(true, $"Version {version}");
            return new CheckResult(true, $"Version {version} (project targets net8.0)");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new CheckResult(false, "dotnet not found in PATH",
                "Install .NET 8 SDK and ensure it is in your system PATH");
        }
        catch (Exception ex)
        {
            StartupLogger.LogException(ex, "CheckDotNet");
            return new CheckResult(false, ex.Message, "Check error_log.txt for details");
        }
    }

    /// <summary>
    /// Check that required NuGet packages are present (via assembly load).
    /// </summary>
    public static CheckResult CheckNuGetPackages()
    {
        var required = new[] { "HelixToolkit.Wpf", "SharpGLTF" };
        var missing = new List<string>();
        foreach (var name in required)
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name?.Contains(name, StringComparison.OrdinalIgnoreCase) == true);
                if (asm == null)
                    missing.Add(name);
            }
            catch
            {
                missing.Add(name);
            }
        }
        if (missing.Count == 0)
            return new CheckResult(true, "HelixToolkit.Wpf, SharpGLTF.Toolkit");
        return new CheckResult(false, $"Missing: {string.Join(", ", missing)}",
            "Run: dotnet restore");
    }

    /// <summary>
    /// Check Blender: config path, auto-detect paths, PATH, and launch verification.
    /// </summary>
    public static CheckResult CheckBlender()
    {
        var pathsToTry = new List<string>();

        // 1. Config
        var config = ConfigService.Load();
        if (!string.IsNullOrEmpty(config.BlenderPath) && File.Exists(config.BlenderPath))
            pathsToTry.Add(config.BlenderPath);

        // 2. Common install locations
        var searchBases = new[]
        {
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\Blender Foundation"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Blender Foundation"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Blender Foundation")
        };
        foreach (var baseDir in searchBases)
        {
            if (!Directory.Exists(baseDir)) continue;
            var dirs = Directory.GetDirectories(baseDir, "Blender *").OrderByDescending(d => d).ToArray();
            foreach (var dir in dirs)
            {
                var exe = Path.Combine(dir, "blender.exe");
                if (File.Exists(exe) && !pathsToTry.Contains(exe))
                    pathsToTry.Add(exe);
            }
        }

        // 3. PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var exe = Path.Combine(dir.Trim(), "blender.exe");
            if (File.Exists(exe) && !pathsToTry.Contains(exe))
                pathsToTry.Add(exe);
        }

        string? foundPath = null;
        foreach (var p in pathsToTry)
        {
            if (File.Exists(p))
            {
                foundPath = p;
                break;
            }
        }

        if (string.IsNullOrEmpty(foundPath))
        {
            return new CheckResult(false, "Blender executable not found",
                "Install Blender or set path in Settings. Example: C:\\Program Files\\Blender Foundation\\Blender 4.0\\blender.exe");
        }

        // Verify we can launch it
        var launchOk = VerifyBlenderCanLaunch(foundPath, out var launchError);
        if (!launchOk)
        {
            return new CheckResult(false, $"Blender found at {foundPath} but cannot be launched: {launchError}",
                "Check permissions or try running as administrator");
        }

        // Optional: version check
        var version = TryGetBlenderVersion(foundPath);
        var msg = version != null ? $"{foundPath} (v{version})" : foundPath;
        return new CheckResult(true, msg);
    }

    /// <summary>
    /// Try to run Blender --version to verify it can be launched.
    /// </summary>
    public static bool VerifyBlenderCanLaunch(string blenderPath, out string? error)
    {
        error = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = BasePath
            };
            using var proc = Process.Start(psi);
            if (proc == null)
            {
                error = "Process.Start returned null";
                return false;
            }
            proc.WaitForExit(15000);
            if (proc.ExitCode != 0 && proc.ExitCode != -1) // -1 can occur on some Blender versions
            {
                var err = proc.StandardError.ReadToEnd();
                error = string.IsNullOrEmpty(err) ? $"Exit code {proc.ExitCode}" : err.Trim();
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            StartupLogger.LogException(ex, "VerifyBlenderCanLaunch");
            return false;
        }
    }

    private static string? TryGetBlenderVersion(string blenderPath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = blenderPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);
            var match = Regex.Match(output, @"Blender\s+(\d+\.\d+(?:\.\d+)?)");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check that essential asset folders and files exist.
    /// </summary>
    public static CheckResult CheckAssets()
    {
        var checks = new (string FullPath, string DisplayPath)[]
        {
            (Path.Combine(BasePath, "assets", "characters", "male_base.glb"), "assets/characters/male_base.glb"),
            (Path.Combine(BasePath, "assets", "characters", "female_base.glb"), "assets/characters/female_base.glb"),
            (Path.Combine(BasePath, "assets", "view_preview", "skin.png"), "assets/view_preview/skin.png"),
            (Path.Combine(BasePath, "assets", "body_parameters.json"), "assets/body_parameters.json")
        };
        var missing = checks.Where(c => !File.Exists(c.FullPath)).Select(c => c.DisplayPath).ToList();
        if (missing.Count == 0)
            return new CheckResult(true, "characters, view_preview, body_parameters.json");
        return new CheckResult(false, $"Missing: {string.Join(", ", missing)}",
            "Ensure assets folder is copied to output. Run: dotnet build");
    }

    /// <summary>
    /// Check that Blender Python scripts exist.
    /// </summary>
    public static CheckResult CheckBlenderScripts()
    {
        var scripts = new[]
        {
            "blender_embed/apply_sculpt_standalone.py",
            "blender_embed/scripts/export_fbx.py"
        };
        var missing = scripts.Where(s => !File.Exists(Path.Combine(BasePath, s))).ToList();
        if (missing.Count == 0)
            return new CheckResult(true, "apply_sculpt_standalone.py, export_fbx.py");
        return new CheckResult(false, $"Missing: {string.Join(", ", missing)}",
            "Ensure blender_embed folder is copied to output. Run: dotnet build");
    }

    /// <summary>
    /// Check read/write access to base directory and config.
    /// </summary>
    public static CheckResult CheckPermissions()
    {
        try
        {
            var testFile = Path.Combine(BasePath, ".perm_test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            var configPath = Path.Combine(BasePath, "config.json");
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return new CheckResult(true, "Read/write OK");
        }
        catch (UnauthorizedAccessException ex)
        {
            return new CheckResult(false, ex.Message, "Run as administrator or change folder permissions");
        }
        catch (Exception ex)
        {
            return new CheckResult(false, ex.Message, "Check folder permissions");
        }
    }
}
