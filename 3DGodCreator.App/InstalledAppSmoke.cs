using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Infrastructure;
using ThreeDGod.Infrastructure.Logging;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

/// <summary>
/// Non-interactive smoke path for installed builds (CI clean-Windows installer).
/// Uses the same Velopack + DI + Config initialization as production, without showing UI.
/// </summary>
public static class InstalledAppSmoke
{
    public const string Arg = "--smoke-test";
    public const string EnvVar = "THREEDGOD_SMOKE_TEST";
    public const string ResultFileName = "smoke-result.json";

    public static bool IsRequested(string[] args) =>
        args.Any(a => string.Equals(a, Arg, StringComparison.OrdinalIgnoreCase))
        || string.Equals(Environment.GetEnvironmentVariable(EnvVar), "1", StringComparison.Ordinal);

    public static int Run(string[] args)
    {
        var baseDir = AppContext.BaseDirectory;
        var resultPath = Path.Combine(baseDir, ResultFileName);
        var errors = new List<string>();
        var checks = new Dictionary<string, object?>();

        try
        {
            AppLogger.Initialize();
            GodLog.Initialize();

            var services = new ServiceCollection();
            services.AddThreeDGodCoreServices();
            using var provider = services.BuildServiceProvider();

            var configService = provider.GetRequiredService<ConfigService>();
            var loaded = configService.Load();
            loaded.Language = "en";
            configService.Save(loaded);
            var reloaded = configService.Load();

            var configPath = Path.Combine(baseDir, "config.json");
            checks["baseDirectory"] = baseDir;
            checks["exeExists"] = File.Exists(Path.Combine(baseDir, "3DGodCreator.App.exe"))
                || File.Exists(Path.Combine(baseDir, "3DGodCreator.App.dll"));
            checks["configWritten"] = File.Exists(configPath);
            checks["configLanguage"] = reloaded.Language;
            checks["projectFolder"] = reloaded.ProjectFolder;
            checks["assetsDir"] = Directory.Exists(Path.Combine(baseDir, "assets"));
            checks["presetsDir"] = Directory.Exists(Path.Combine(baseDir, "presets"));
            checks["updateExe"] = File.Exists(Path.Combine(baseDir, "Update.exe"))
                || File.Exists(Path.Combine(Directory.GetParent(baseDir.TrimEnd('\\', '/'))?.FullName ?? baseDir, "Update.exe"));

            if (checks["exeExists"] is false)
                errors.Add("Installed main assembly/exe missing next to BaseDirectory.");
            if (checks["configWritten"] is false)
                errors.Add("config.json was not created beside the installed app.");
            if (!string.Equals(reloaded.Language, "en", StringComparison.Ordinal))
                errors.Add("Config round-trip failed (language).");
            if (string.IsNullOrWhiteSpace(reloaded.ProjectFolder) || !Directory.Exists(reloaded.ProjectFolder))
                errors.Add("ProjectFolder was not recovered/created under LocalApplicationData.");
            if (checks["assetsDir"] is false)
                errors.Add("assets/ missing from installed package (publish packaging incomplete).");
            if (checks["presetsDir"] is false)
                errors.Add("presets/ missing from installed package (publish packaging incomplete).");

            var ok = errors.Count == 0;
            var payload = new
            {
                ok,
                mode = "installed-app-smoke",
                args,
                checks,
                errors,
                utc = DateTime.UtcNow.ToString("o")
            };
            File.WriteAllText(resultPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            Console.Error.WriteLine(ok ? "SMOKE_STATUS=SUCCESS" : "SMOKE_STATUS=FAILED");
            foreach (var e in errors)
                Console.Error.WriteLine("SMOKE_ERROR: " + e);
            AppLogger.SetShutdownReason(ok ? "SMOKE_OK" : "SMOKE_FAIL");
            return ok ? 0 : 2;
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(resultPath, JsonSerializer.Serialize(new
                {
                    ok = false,
                    mode = "installed-app-smoke",
                    error = ex.ToString(),
                    utc = DateTime.UtcNow.ToString("o")
                }));
            }
            catch { /* ignore */ }
            Console.Error.WriteLine("SMOKE_STATUS=CRASH");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
