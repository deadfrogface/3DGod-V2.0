using ThreeDGod.Workers;

namespace ThreeDGod.Infrastructure;

/// <summary>
/// Resolves product install / data roots for zero-manual-dependency packaging.
/// Prefer shipped AppBase workers over a developer git checkout.
/// </summary>
public static class InstallLayout
{
    public const string AppFolderName = "3DGod";

    public static string DataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);

    public static string ModelsRoot => Path.Combine(DataRoot, "Models");
    public static string ComponentsRoot => Path.Combine(DataRoot, "Components");
    public static string LogsRoot => Path.Combine(DataRoot, "Logs");
    public static string UvRoot => Path.Combine(DataRoot, "Tools", "uv");
    public static string RecoveryRoot => Path.Combine(DataRoot, "Recovery");

    /// <summary>Directory containing the running application binaries (Velopack publish output).</summary>
    public static string AppBaseDirectory =>
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>
    /// Content root that contains <c>workers/</c>. Prefer AppBase (packaged), then walk up from cwd/repo.
    /// </summary>
    public static string ResolveContentRoot(string? overrideRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(overrideRoot) && Directory.Exists(Path.Combine(overrideRoot, "workers")))
            return Path.GetFullPath(overrideRoot);

        var appWorkers = Path.Combine(AppBaseDirectory, "workers");
        if (Directory.Exists(appWorkers))
            return AppBaseDirectory;

        // Dev: AppBase is bin/Release/... — walk up for repo root.
        var dir = new DirectoryInfo(AppBaseDirectory);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "workers", "anny")))
                return dir.FullName;
        }

        try
        {
            return AnnyRuntime.FindRepoRoot();
        }
        catch
        {
            return AppBaseDirectory;
        }
    }

    public static string WorkerProjectDir(string componentId, string? contentRoot = null) =>
        Path.Combine(ResolveContentRoot(contentRoot), "workers", componentId);

    public static string SkinTokensCppCliPath(string? contentRoot = null)
    {
        var root = ResolveContentRoot(contentRoot);
        var candidates = new[]
        {
            Path.Combine(ComponentsRoot, "skintokens-cpp", "bin", "skintokens-cli.exe"),
            Path.Combine(ComponentsRoot, "skintokens-cpp", "bin", "skintokens-cli"),
            Path.Combine(ModelsRoot, "skintokens-cpp", "bin", "skintokens-cli.exe"),
            Path.Combine(ModelsRoot, "skintokens-cpp", "bin", "skintokens-cli"),
            Path.Combine(root, "workers", "skintokens-cpp", "dist", "bin", "skintokens-cli.exe"),
            Path.Combine(root, "workers", "skintokens-cpp", "dist", "bin", "skintokens-cli"),
            Path.Combine(root, "third_party", "skin-tokens.cpp", "dist", "bin", "skintokens-cli"),
            Path.Combine(root, "third_party", "skin-tokens.cpp", "build", "release", "bin", "skintokens-cli")
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    public static string? FindSkinTokensCppModelDir()
    {
        var candidates = new[]
        {
            Path.Combine(ModelsRoot, "skintokens-cpp", "F16"),
            Path.Combine(ModelsRoot, "SkinTokens-GGUF", "F16"),
            Path.Combine(ModelsRoot, "skintokens-cpp", "SkinTokens-GGUF", "F16"),
            Path.Combine(ResolveContentRoot(), "workers", "skintokens-cpp", "models", "SkinTokens-GGUF", "F16")
        };
        return candidates.FirstOrDefault(d =>
            Directory.Exists(d) && Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories).Any());
    }
}
