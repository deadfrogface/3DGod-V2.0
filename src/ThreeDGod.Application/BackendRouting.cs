namespace ThreeDGod.Application;

public enum BackendRuntimeState
{
    Available,
    Unavailable,
    LicenseBlocked,
    InsufficientVram,
    UnsupportedHardware,
    NotInstalled
}

public sealed class HardwareRequirement
{
    public int MinVramMb { get; init; }
    public bool RequiresCuda { get; init; }
}

public sealed class LicenseProfile
{
    public string Id { get; init; } = "";
    public bool Accepted { get; init; }
}

public sealed class BackendCapabilities
{
    public bool HumanGenerate { get; init; }
    public bool ImageTo3d { get; init; }
}

public sealed class BackendManifest
{
    public string Id { get; init; } = "";
    public int Priority { get; init; }
    public BackendRuntimeState State { get; init; }
    public HardwareRequirement Hardware { get; init; } = new();
    public LicenseProfile License { get; init; } = new();
    public BackendCapabilities Capabilities { get; init; } = new();
}

public sealed class BackendRouteResult
{
    public BackendManifest? Provider { get; init; }
    public string Reason { get; init; } = "";
}

public interface IBackendRegistry
{
    IReadOnlyList<BackendManifest> List();
}

public sealed class BackendRegistry : IBackendRegistry
{
    private readonly List<BackendManifest> _backends;

    public BackendRegistry(IEnumerable<BackendManifest> backends) => _backends = backends.ToList();

    public IReadOnlyList<BackendManifest> List() => _backends;
}

public sealed class BackendRouter
{
    public BackendRouteResult Route(IEnumerable<BackendManifest> backends, int availableVramMb, bool cudaAvailable, string capability)
    {
        foreach (var backend in backends.OrderByDescending(b => b.Priority))
        {
            var (ok, reason) = Evaluate(backend, availableVramMb, cudaAvailable, capability);
            if (ok)
                return new BackendRouteResult { Provider = backend, Reason = reason };
        }

        return new BackendRouteResult { Provider = null, Reason = "no eligible backend" };
    }

    private static (bool Ok, string Reason) Evaluate(BackendManifest backend, int vram, bool cuda, string capability)
    {
        if (backend.State == BackendRuntimeState.Unavailable)
            return (false, "unavailable excluded");
        if (backend.State == BackendRuntimeState.NotInstalled)
            return (false, "not installed excluded");
        if (backend.State == BackendRuntimeState.LicenseBlocked || !backend.License.Accepted)
            return (false, "license blocked excluded");
        if (backend.State == BackendRuntimeState.InsufficientVram || vram < backend.Hardware.MinVramMb)
            return (false, "insufficient VRAM excluded");
        if (backend.Hardware.RequiresCuda && !cuda)
            return (false, "unsupported hardware excluded");
        if (backend.State == BackendRuntimeState.UnsupportedHardware)
            return (false, "unsupported hardware excluded");
        if (!CapabilityMatches(backend, capability))
            return (false, "capability mismatch");
        return (true, "selected");
    }

    private static bool CapabilityMatches(BackendManifest backend, string capability) =>
        capability switch
        {
            "human.generate" => backend.Capabilities.HumanGenerate,
            "image.to3d" => backend.Capabilities.ImageTo3d,
            _ => false
        };
}
