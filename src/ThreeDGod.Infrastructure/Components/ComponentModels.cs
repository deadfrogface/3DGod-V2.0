namespace ThreeDGod.Infrastructure.Components;

/// <summary>Product-facing component lifecycle states (honest, never fake Ready).</summary>
public enum ComponentState
{
    Ready,
    NotInstalled,
    Installing,
    UpdateAvailable,
    HardwareUnsupported,
    LicenseBlocked,
    DownloadUnavailable,
    Broken,
    Optional,
    Unknown
}

/// <summary>Allowlisted health-check kinds. Manifests cannot invent shell commands.</summary>
public enum ComponentHealthCheckKind
{
    FileExists,
    WorkerPing,
    PythonImport,
    ExecutableVersion,
    ModelLoad,
    BlenderBackground,
    InferenceFixture
}

public sealed class ComponentHealthCheck
{
    public ComponentHealthCheckKind Kind { get; init; }
    public string Target { get; init; } = "";
    public string? ExpectContains { get; init; }
}

public sealed class ComponentDependency
{
    public string ComponentId { get; init; } = "";
    public bool Optional { get; init; }
}

public sealed class ComponentHardwareRequirements
{
    public int MinVramMb { get; init; }
    public bool RequiresCuda { get; init; }
    public long MinDiskFreeBytes { get; init; }
    public long MinRamBytes { get; init; }
}

public sealed class ComponentManifest
{
    public int ManifestVersion { get; init; } = 1;
    public string ComponentId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string PackageVersion { get; init; } = "";
    public string LicenseId { get; init; } = "";
    public bool LicenseAcceptedRequired { get; init; } = true;
    public bool Optional { get; init; } = true;
    public string? DownloadUrl { get; init; }
    public string? Sha256 { get; init; }
    public string? LocalSourceHint { get; init; }
    public ComponentHardwareRequirements Hardware { get; init; } = new();
    public IReadOnlyList<ComponentDependency> Dependencies { get; init; } = [];
    public IReadOnlyList<ComponentHealthCheck> HealthChecks { get; init; } = [];
    public string? FeatureId { get; init; }
    public string? Notes { get; init; }
}

public sealed class ComponentRecord
{
    public string ComponentId { get; init; } = "";
    public string Version { get; init; } = "";
    public string InstallPath { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public bool LicenseAccepted { get; init; }
    public ComponentState State { get; init; } = ComponentState.Unknown;
    public string Message { get; init; } = "";
}

public sealed class ComponentHealthReport
{
    public bool Ok { get; init; }
    public ComponentState State { get; init; }
    public string Message { get; init; } = "";
    public IReadOnlyList<string> Details { get; init; } = [];
}

public sealed class ComponentDiagnostics
{
    public string ComponentId { get; init; } = "";
    public ComponentState State { get; init; }
    public string InstallPath { get; init; } = "";
    public string Message { get; init; } = "";
    public IReadOnlyList<string> HealthDetails { get; init; } = [];
    public HardwareProfile? Hardware { get; init; }
}
