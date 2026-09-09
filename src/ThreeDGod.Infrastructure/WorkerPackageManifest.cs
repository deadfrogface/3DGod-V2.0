using System.Text.Json;

namespace ThreeDGod.Infrastructure;

public sealed class WorkerPackageManifest
{
    public int ManifestVersion { get; init; }
    public string WorkerId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Status { get; init; } = "";
    public string PackageVersion { get; init; } = "";
    public string LicenseId { get; init; } = "";
    public string? LicenseRef { get; init; }
    public bool IncludedInBaseInstaller { get; init; }
    public WorkerPythonPackage? PythonPackage { get; init; }
    public WorkerModelMetadata? Model { get; init; }
    public WorkerReleasePackage? ReleasePackage { get; init; }
    public WorkerRequirements Requirements { get; init; } = new();
}

public sealed class WorkerPythonPackage
{
    public string? Name { get; init; }
    public string? PinnedVersion { get; init; }
    public string? UvLock { get; init; }
    public string? Pyproject { get; init; }
}

public sealed class WorkerModelMetadata
{
    public string? CheckpointHash { get; init; }
    public long SizeBytes { get; init; }
    public bool Verified { get; init; }
}

public sealed class WorkerReleasePackage
{
    public string? Format { get; init; }
    public string? DownloadUrl { get; init; }
    public string? Sha256 { get; init; }
}

public sealed class WorkerRequirements
{
    public int MinVramMb { get; init; }
    public bool RequiresCuda { get; init; }
    public bool RequiresUv { get; init; }
    public bool RequiresBlender { get; init; }
    public string? Notes { get; init; }
}

public sealed class WorkerManifestValidationResult
{
    public bool Ok { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

public static class WorkerPackageManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly HashSet<string> AllowedStatuses =
    [
        "Available", "Experimental", "NotInstalled", "LicenseBlocked", "UnsupportedHardware"
    ];

    public static WorkerPackageManifest Load(string path)
    {
        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<WorkerPackageManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Manifest parse failed: {path}");
        return manifest;
    }

    public static WorkerManifestValidationResult Validate(WorkerPackageManifest manifest, string repoRoot)
    {
        var errors = new List<string>();
        if (manifest.ManifestVersion != 1)
            errors.Add($"manifestVersion must be 1 (got {manifest.ManifestVersion})");
        if (string.IsNullOrWhiteSpace(manifest.WorkerId))
            errors.Add("workerId is required");
        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
            errors.Add("displayName is required");
        if (!AllowedStatuses.Contains(manifest.Status))
            errors.Add($"status '{manifest.Status}' is not allowed");
        if (string.IsNullOrWhiteSpace(manifest.PackageVersion))
            errors.Add("packageVersion is required");
        if (string.IsNullOrWhiteSpace(manifest.LicenseId))
            errors.Add("licenseId is required");
        if (manifest.Status is "Available" or "LicenseBlocked")
        {
            if (ReleaseLicenseGate.IsBlockedLicenseId(manifest.LicenseId))
                errors.Add($"licenseId '{manifest.LicenseId}' is blocklisted for production");
            if (ReleaseLicenseGate.IsBlockedModelId(manifest.WorkerId))
                errors.Add($"workerId '{manifest.WorkerId}' is blocklisted for production");
        }
        if (manifest.IncludedInBaseInstaller)
            errors.Add("includedInBaseInstaller must be false (AI models/workers are not forced into base installer)");
        if (manifest.Model?.Verified == false && manifest.Status is "Available")
            errors.Add("Available status requires verified model metadata or explicit demo-only worker");
        if (manifest.PythonPackage?.UvLock is { Length: > 0 } lockRel &&
            !File.Exists(Path.Combine(repoRoot, lockRel.Replace('/', Path.DirectorySeparatorChar))))
            errors.Add($"uvLock missing: {lockRel}");
        return new WorkerManifestValidationResult { Ok = errors.Count == 0, Errors = errors };
    }

    public static IReadOnlyList<string> DiscoverManifestPaths(string repoRoot)
    {
        var paths = new List<string>();
        var workersDir = Path.Combine(repoRoot, "workers");
        if (Directory.Exists(workersDir))
        {
            foreach (var dir in Directory.GetDirectories(workersDir))
            {
                var manifest = Path.Combine(dir, "manifest.json");
                if (File.Exists(manifest))
                    paths.Add(manifest);
            }
        }

        var packagingDir = Path.Combine(repoRoot, "docs", "packaging", "workers");
        if (Directory.Exists(packagingDir))
            paths.AddRange(Directory.GetFiles(packagingDir, "*.manifest.json"));
        return paths;
    }
}
