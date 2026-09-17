namespace ThreeDGod.Infrastructure.Components;

/// <summary>
/// End-user install path for uv-managed workers (Anny / GarmentCode).
/// Uses the same workers/*/pyproject.toml + uv.lock as CI — no alternate dependency set.
/// </summary>
public interface IWorkerUvComponentInstaller
{
    Task<ComponentRecord> InstallOrRepairAsync(
        string componentId,
        bool acceptLicense,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class WorkerUvComponentInstaller : IWorkerUvComponentInstaller
{
    private readonly IComponentManager _components;
    private readonly IUvProvisioner _uv;
    private readonly IComponentHealthCheckRunner _health;
    private readonly ISkinTokensCppProvisioner? _skinTokensCpp;
    private readonly string _repoRoot;

    public WorkerUvComponentInstaller(
        IComponentManager components,
        IUvProvisioner uv,
        string? repoRoot = null,
        IComponentHealthCheckRunner? health = null,
        ISkinTokensCppProvisioner? skinTokensCpp = null)
    {
        _components = components;
        _uv = uv;
        _health = health ?? new ComponentHealthCheckRunner();
        _skinTokensCpp = skinTokensCpp;
        _repoRoot = repoRoot ?? FindRepoRoot();
    }

    public async Task<ComponentRecord> InstallOrRepairAsync(
        string componentId,
        bool acceptLicense,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ModelManager.ValidatePackageId(componentId);
        var manifest = _components.ListManifests()
            .FirstOrDefault(m => string.Equals(m.ComponentId, componentId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown component '{componentId}'.");

        if (!acceptLicense && manifest.LicenseAcceptedRequired)
        {
            return new ComponentRecord
            {
                ComponentId = componentId,
                State = ComponentState.LicenseBlocked,
                Message = "License not accepted."
            };
        }

        if (string.Equals(componentId, "skintokens-cpp", StringComparison.OrdinalIgnoreCase))
            return await InstallSkinTokensCppAsync(manifest, acceptLicense, progress, cancellationToken);

        progress?.Report("Ensuring pinned uv…");
        IProgress<ComponentDownloadProgress>? downloadProgress = progress is null
            ? null
            : new Progress<ComponentDownloadProgress>(p =>
                progress.Report(p.Percent is double pct
                    ? $"Downloading uv… {pct:0.0}%"
                    : $"Downloading uv… {p.BytesReceived} bytes"));

        var uv = await _uv.EnsureUvAsync(downloadProgress, cancellationToken);
        progress?.Report($"uv {uv.Version} ready at {uv.UvPath}");

        var projectDir = Path.Combine(_repoRoot, "workers", componentId);
        if (!Directory.Exists(projectDir))
        {
            return new ComponentRecord
            {
                ComponentId = componentId,
                State = ComponentState.DownloadUnavailable,
                Message = $"Worker project missing at workers/{componentId}."
            };
        }

        progress?.Report($"uv sync --frozen ({componentId})…");
        var syncProgress = progress is null ? null : new Progress<string>(progress.Report);
        var sync = await _uv.SyncWorkerAsync(projectDir, syncProgress, cancellationToken);
        if (!sync.Ok)
        {
            return new ComponentRecord
            {
                ComponentId = componentId,
                InstallPath = projectDir,
                State = ComponentState.Broken,
                Message = sync.Message,
                LicenseAccepted = acceptLicense
            };
        }

        var markerRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod",
            "Models",
            componentId);
        Directory.CreateDirectory(markerRoot);
        foreach (var name in new[]
                 {
                     "uv.lock", "pyproject.toml",
                     $"{componentId}_worker.py",
                     "anny_worker.py", "garmentcode_worker.py", "echo_worker.py",
                     "flux_worker.py", "skintokens_worker.py", "triposr_worker.py"
                 })
        {
            var src = Path.Combine(projectDir, name);
            if (File.Exists(src))
                File.Copy(src, Path.Combine(markerRoot, Path.GetFileName(src)), overwrite: true);
        }

        File.WriteAllText(
            Path.Combine(markerRoot, ".3dgod-component.json"),
            $"{{\"componentId\":\"{componentId}\",\"packageVersion\":\"{manifest.PackageVersion}\",\"uvVersion\":\"{uv.Version}\",\"venv\":\"{sync.VenvDir.Replace("\\", "\\\\")}\",\"installedUtc\":\"{DateTime.UtcNow:O}\"}}");

        var health = _health.Run(markerRoot, manifest.HealthChecks, cancellationToken);
        return new ComponentRecord
        {
            ComponentId = componentId,
            Version = manifest.PackageVersion,
            InstallPath = markerRoot,
            LicenseAccepted = true,
            State = health.Ok ? ComponentState.Ready : ComponentState.Broken,
            Message = health.Ok
                ? $"uv environment ready ({sync.VenvDir})."
                : "Post-sync health check failed: " + health.Message
        };
    }

    private async Task<ComponentRecord> InstallSkinTokensCppAsync(
        ComponentManifest manifest,
        bool acceptLicense,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        if (_skinTokensCpp is null)
        {
            return new ComponentRecord
            {
                ComponentId = manifest.ComponentId,
                State = ComponentState.Broken,
                Message = "SkinTokensCppProvisioner not registered."
            };
        }

        progress?.Report("Provisioning skin-tokens.cpp models + CLI…");
        var result = await _skinTokensCpp.EnsureAsync(acceptLicense, progress, cancellationToken);
        var installPath = Path.Combine(InstallLayout.ComponentsRoot, "skintokens-cpp");
        Directory.CreateDirectory(installPath);
        File.WriteAllText(
            Path.Combine(installPath, ".3dgod-component.json"),
            $"{{\"componentId\":\"skintokens-cpp\",\"packageVersion\":\"{manifest.PackageVersion}\",\"cli\":\"{(result.CliPath ?? "").Replace("\\", "\\\\")}\",\"modelDir\":\"{(result.ModelDir ?? "").Replace("\\", "\\\\")}\",\"upstream\":\"{SkinTokensCppRuntime.UpstreamCommitHint}\",\"installedUtc\":\"{DateTime.UtcNow:O}\"}}");

        // Health checks target the worker source tree (README), not Components — copy README marker.
        var readmeSrc = Path.Combine(_repoRoot, "workers", "skintokens-cpp", "README.md");
        if (File.Exists(readmeSrc))
            File.Copy(readmeSrc, Path.Combine(installPath, "README.md"), overwrite: true);

        return new ComponentRecord
        {
            ComponentId = manifest.ComponentId,
            Version = manifest.PackageVersion,
            InstallPath = installPath,
            LicenseAccepted = acceptLicense,
            State = result.Ok ? ComponentState.Ready : ComponentState.Broken,
            Message = result.Message
        };
    }

    private static string FindRepoRoot()
    {
        var env = Environment.GetEnvironmentVariable("THREEDGOD_CONTENT_ROOT");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(Path.Combine(env, "workers")))
            return Path.GetFullPath(env);

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "3DGodCreator.sln")) ||
                File.Exists(Path.Combine(dir.FullName, "3DGod.sln")) ||
                Directory.Exists(Path.Combine(dir.FullName, "workers")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }
}
