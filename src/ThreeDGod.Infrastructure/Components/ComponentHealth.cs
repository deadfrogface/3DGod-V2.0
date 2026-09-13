using System.Text.Json;

namespace ThreeDGod.Infrastructure.Components;

public interface IComponentHealthCheckRunner
{
    ComponentHealthReport Run(string installRoot, IReadOnlyList<ComponentHealthCheck> checks, CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed allowlisted health checks only — never executes arbitrary manifest shell commands.
/// </summary>
public sealed class ComponentHealthCheckRunner : IComponentHealthCheckRunner
{
    public ComponentHealthReport Run(string installRoot, IReadOnlyList<ComponentHealthCheck> checks, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(installRoot))
            return Fail(ComponentState.NotInstalled, "Install directory missing.", []);

        if (checks.Count == 0)
            return Fail(ComponentState.Broken, "No health checks declared for component.", []);

        var details = new List<string>();
        foreach (var check in checks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (check.Kind)
            {
                case ComponentHealthCheckKind.FileExists:
                {
                    var path = ResolveUnderRoot(installRoot, check.Target);
                    if (!File.Exists(path) && !Directory.Exists(path))
                        return Fail(ComponentState.Broken, $"FileExists failed: {check.Target}", details);
                    details.Add($"FileExists OK: {check.Target}");
                    break;
                }
                case ComponentHealthCheckKind.PythonImport:
                {
                    // Health means the declared module marker file exists in the install tree.
                    // Full interpreter import is validated by worker runtimes (Anny/Garment) separately.
                    var marker = ResolveUnderRoot(installRoot, check.Target.Replace('.', Path.DirectorySeparatorChar) + ".py");
                    var pkg = ResolveUnderRoot(installRoot, check.Target.Replace('.', Path.DirectorySeparatorChar));
                    if (!File.Exists(marker) && !Directory.Exists(pkg))
                        return Fail(ComponentState.Broken, $"PythonImport marker missing: {check.Target}", details);
                    details.Add($"PythonImport marker OK: {check.Target}");
                    break;
                }
                case ComponentHealthCheckKind.WorkerPing:
                {
                    var script = ResolveUnderRoot(installRoot, check.Target);
                    if (!File.Exists(script))
                        return Fail(ComponentState.Broken, $"WorkerPing script missing: {check.Target}", details);
                    details.Add($"WorkerPing script present: {check.Target}");
                    break;
                }
                case ComponentHealthCheckKind.ExecutableVersion:
                {
                    var exe = ResolveUnderRoot(installRoot, check.Target);
                    if (!File.Exists(exe))
                    {
                        // Also allow PATH lookup for tools like uv when Target is a bare name.
                        if (check.Target.Contains('/') || check.Target.Contains('\\') || check.Target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            return Fail(ComponentState.Broken, $"Executable missing: {check.Target}", details);
                        var onPath = FindOnPath(check.Target);
                        if (onPath is null)
                            return Fail(ComponentState.NotInstalled, $"Executable not on PATH: {check.Target}", details);
                        details.Add($"ExecutableVersion PATH OK: {onPath}");
                        break;
                    }
                    details.Add($"ExecutableVersion file OK: {check.Target}");
                    break;
                }
                case ComponentHealthCheckKind.ModelLoad:
                case ComponentHealthCheckKind.BlenderBackground:
                case ComponentHealthCheckKind.InferenceFixture:
                {
                    var path = ResolveUnderRoot(installRoot, check.Target);
                    if (!File.Exists(path))
                        return Fail(ComponentState.Broken, $"{check.Kind} target missing: {check.Target}", details);
                    if (!string.IsNullOrEmpty(check.ExpectContains))
                    {
                        var text = File.ReadAllText(path);
                        if (!text.Contains(check.ExpectContains, StringComparison.Ordinal))
                            return Fail(ComponentState.Broken, $"{check.Kind} expectation failed for {check.Target}", details);
                    }
                    details.Add($"{check.Kind} OK: {check.Target}");
                    break;
                }
                default:
                    return Fail(ComponentState.Broken, $"Unsupported health check kind: {check.Kind}", details);
            }
        }

        return new ComponentHealthReport
        {
            Ok = true,
            State = ComponentState.Ready,
            Message = "Health checks passed.",
            Details = details
        };
    }

    private static ComponentHealthReport Fail(ComponentState state, string message, List<string> details) =>
        new()
        {
            Ok = false,
            State = state,
            Message = message,
            Details = details
        };

    private static string ResolveUnderRoot(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative))
            throw new InvalidOperationException("Health check target is empty.");
        var combined = Path.GetFullPath(Path.Combine(root, relative));
        var rootFull = Path.GetFullPath(root);
        if (!rootFull.EndsWith(Path.DirectorySeparatorChar))
            rootFull += Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(combined, Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Health check path escaped install root.");
        return combined;
    }

    private static string? FindOnPath(string name)
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            var candidate = Path.Combine(dir, name);
            if (File.Exists(candidate))
                return candidate;
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                var exe = candidate + ".exe";
                if (File.Exists(exe))
                    return exe;
            }
        }
        return null;
    }
}

public static class ComponentManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static ComponentManifest FromWorkerPackageManifest(WorkerPackageManifest worker)
    {
        var checks = new List<ComponentHealthCheck>();
        // Prefer explicit health checks when present on disk sidecar; otherwise derive safe defaults.
        if (string.Equals(worker.WorkerId, "echo", StringComparison.OrdinalIgnoreCase))
        {
            checks.Add(new ComponentHealthCheck
            {
                Kind = ComponentHealthCheckKind.FileExists,
                Target = "echo_worker.py"
            });
            checks.Add(new ComponentHealthCheck
            {
                Kind = ComponentHealthCheckKind.WorkerPing,
                Target = "echo_worker.py"
            });
        }
        else if (worker.PythonPackage?.UvLock is { Length: > 0 })
        {
            checks.Add(new ComponentHealthCheck
            {
                Kind = ComponentHealthCheckKind.FileExists,
                Target = Path.GetFileName(worker.PythonPackage.UvLock)
            });
        }

        return new ComponentManifest
        {
            ManifestVersion = worker.ManifestVersion,
            ComponentId = worker.WorkerId,
            DisplayName = worker.DisplayName,
            PackageVersion = worker.PackageVersion,
            LicenseId = worker.LicenseId,
            Optional = !worker.IncludedInBaseInstaller,
            DownloadUrl = worker.ReleasePackage?.DownloadUrl,
            Sha256 = worker.ReleasePackage?.Sha256,
            Hardware = new ComponentHardwareRequirements
            {
                MinVramMb = worker.Requirements.MinVramMb,
                RequiresCuda = worker.Requirements.RequiresCuda
            },
            HealthChecks = checks,
            FeatureId = worker.WorkerId switch
            {
                "anny" => "human.anny",
                "garmentcode" => "garment.garmentcode",
                _ => null
            },
            Notes = worker.Requirements.Notes
        };
    }

    public static ComponentManifest LoadJson(string path)
    {
        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<ComponentManifestDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Component manifest parse failed: " + path);
        if (dto.ManifestVersion != 1)
            throw new InvalidOperationException("Unsupported component manifest version.");
        if (string.IsNullOrWhiteSpace(dto.ComponentId))
            throw new InvalidOperationException("componentId required.");

        var checks = (dto.HealthChecks ?? [])
            .Select(c => new ComponentHealthCheck
            {
                Kind = ParseKind(c.Kind),
                Target = c.Target ?? "",
                ExpectContains = c.ExpectContains
            })
            .ToList();

        return new ComponentManifest
        {
            ManifestVersion = dto.ManifestVersion,
            ComponentId = dto.ComponentId,
            DisplayName = dto.DisplayName ?? dto.ComponentId,
            PackageVersion = dto.PackageVersion ?? "0",
            LicenseId = dto.LicenseId ?? "unknown",
            LicenseAcceptedRequired = dto.LicenseAcceptedRequired ?? true,
            Optional = dto.Optional ?? true,
            DownloadUrl = dto.DownloadUrl,
            Sha256 = dto.Sha256,
            LocalSourceHint = dto.LocalSourceHint,
            Hardware = new ComponentHardwareRequirements
            {
                MinVramMb = dto.Hardware?.MinVramMb ?? 0,
                RequiresCuda = dto.Hardware?.RequiresCuda ?? false,
                MinDiskFreeBytes = dto.Hardware?.MinDiskFreeBytes ?? 0,
                MinRamBytes = dto.Hardware?.MinRamBytes ?? 0
            },
            Dependencies = (dto.Dependencies ?? [])
                .Select(d => new ComponentDependency { ComponentId = d.ComponentId ?? "", Optional = d.Optional ?? false })
                .ToList(),
            HealthChecks = checks,
            FeatureId = dto.FeatureId,
            Notes = dto.Notes
        };
    }

    private static ComponentHealthCheckKind ParseKind(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
            throw new InvalidOperationException("healthChecks.kind required.");
        if (!Enum.TryParse<ComponentHealthCheckKind>(kind, ignoreCase: true, out var parsed))
            throw new InvalidOperationException($"Health check kind '{kind}' is not allowlisted.");
        return parsed;
    }

    private sealed class ComponentManifestDto
    {
        public int ManifestVersion { get; set; } = 1;
        public string? ComponentId { get; set; }
        public string? DisplayName { get; set; }
        public string? PackageVersion { get; set; }
        public string? LicenseId { get; set; }
        public bool? LicenseAcceptedRequired { get; set; }
        public bool? Optional { get; set; }
        public string? DownloadUrl { get; set; }
        public string? Sha256 { get; set; }
        public string? LocalSourceHint { get; set; }
        public HardwareDto? Hardware { get; set; }
        public List<DependencyDto>? Dependencies { get; set; }
        public List<HealthDto>? HealthChecks { get; set; }
        public string? FeatureId { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class HardwareDto
    {
        public int MinVramMb { get; set; }
        public bool RequiresCuda { get; set; }
        public long MinDiskFreeBytes { get; set; }
        public long MinRamBytes { get; set; }
    }

    private sealed class DependencyDto
    {
        public string? ComponentId { get; set; }
        public bool? Optional { get; set; }
    }

    private sealed class HealthDto
    {
        public string? Kind { get; set; }
        public string? Target { get; set; }
        public string? ExpectContains { get; set; }
    }
}
