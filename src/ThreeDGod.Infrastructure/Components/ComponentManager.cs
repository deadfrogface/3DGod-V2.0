using ThreeDGod.Persistence;

namespace ThreeDGod.Infrastructure.Components;

public interface IComponentManager
{
    IReadOnlyList<ComponentManifest> ListManifests();
    ComponentRecord GetState(string componentId);
    Task<ComponentRecord> InstallFromZipAsync(ComponentManifest manifest, string sourceZip, bool acceptLicense, CancellationToken cancellationToken = default);
    Task<ComponentRecord> RepairAsync(ComponentManifest manifest, string sourceZip, CancellationToken cancellationToken = default);
    ComponentHealthReport Verify(ComponentManifest manifest);
    void Remove(string componentId);
    bool TryRollback(string componentId);
    ComponentDiagnostics Diagnose(ComponentManifest manifest);
}

/// <summary>
/// Manifest-driven component backend built on <see cref="ModelManager"/> install/security primitives.
/// Demo-specific echo_worker checks live only in manifests / derived health checks — not in generic verify logic.
/// </summary>
public sealed class ComponentManager : IComponentManager
{
    private readonly string _root;
    private readonly ModelManager _models;
    private readonly IComponentHealthCheckRunner _health;
    private readonly Func<HardwareProfile> _hardware;
    private readonly List<ComponentManifest> _manifests = [];
    private readonly Dictionary<string, ComponentState> _transient = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public ComponentManager(
        string root,
        IEnumerable<ComponentManifest>? manifests = null,
        IComponentHealthCheckRunner? health = null,
        ArchiveLimits? limits = null,
        Func<HardwareProfile>? hardware = null)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
        _models = new ModelManager(_root, limits);
        _health = health ?? new ComponentHealthCheckRunner();
        _hardware = hardware ?? HardwareProfiler.Probe;
        if (manifests != null)
            _manifests.AddRange(manifests);
    }

    public static ComponentManager FromRepo(string repoRoot, string installRoot)
    {
        var manifests = new List<ComponentManifest>();
        foreach (var path in WorkerPackageManifestReader.DiscoverManifestPaths(repoRoot))
        {
            try
            {
                var worker = WorkerPackageManifestReader.Load(path);
                var validation = WorkerPackageManifestReader.Validate(worker, repoRoot);
                if (!validation.Ok)
                    continue;
                var componentJson = Path.Combine(Path.GetDirectoryName(path)!, "component.manifest.json");
                manifests.Add(File.Exists(componentJson)
                    ? ComponentManifestLoader.LoadJson(componentJson)
                    : ComponentManifestLoader.FromWorkerPackageManifest(worker));
            }
            catch
            {
                // Skip invalid packaging docs; never crash host startup.
            }
        }

        return new ComponentManager(installRoot, manifests);
    }

    public IReadOnlyList<ComponentManifest> ListManifests() => _manifests.ToList();

    public ComponentRecord GetState(string componentId)
    {
        ModelManager.ValidatePackageId(componentId);
        var manifest = FindManifest(componentId);
        lock (_gate)
        {
            if (_transient.TryGetValue(componentId, out var transient) && transient == ComponentState.Installing)
            {
                return new ComponentRecord
                {
                    ComponentId = componentId,
                    InstallPath = Path.Combine(_root, componentId),
                    State = ComponentState.Installing,
                    Message = "Installation in progress."
                };
            }
        }

        var installPath = Path.Combine(_root, componentId);
        if (!Directory.Exists(installPath))
        {
            if (manifest?.Optional == true)
                return NotInstalled(componentId, installPath, ComponentState.Optional, "Optional component is not installed.");
            if (!string.IsNullOrWhiteSpace(manifest?.DownloadUrl))
                return NotInstalled(componentId, installPath, ComponentState.NotInstalled, "Component not installed; download available.");
            return NotInstalled(componentId, installPath, ComponentState.DownloadUnavailable, "Component not installed and no download URL is configured.");
        }

        if (manifest is null)
        {
            return new ComponentRecord
            {
                ComponentId = componentId,
                InstallPath = installPath,
                State = ComponentState.Unknown,
                Message = "Installed files present but no manifest is loaded."
            };
        }

        var gate = EvaluateGates(manifest);
        if (gate is not null)
        {
            return new ComponentRecord
            {
                ComponentId = gate.ComponentId,
                Version = gate.Version,
                InstallPath = installPath,
                Sha256 = gate.Sha256,
                LicenseAccepted = gate.LicenseAccepted,
                State = gate.State,
                Message = gate.Message
            };
        }

        var health = _health.Run(installPath, manifest.HealthChecks);
        return new ComponentRecord
        {
            ComponentId = componentId,
            Version = manifest.PackageVersion,
            InstallPath = installPath,
            State = health.State,
            Message = health.Message,
            LicenseAccepted = true
        };
    }

    public async Task<ComponentRecord> InstallFromZipAsync(
        ComponentManifest manifest,
        string sourceZip,
        bool acceptLicense,
        CancellationToken cancellationToken = default)
    {
        ModelManager.ValidatePackageId(manifest.ComponentId);
        EnsureManifestTracked(manifest);

        var gate = EvaluateGates(manifest, acceptLicense);
        if (gate is not null)
            return gate;

        var dest = Path.Combine(_root, manifest.ComponentId);
        var backup = dest + ".bak";
        lock (_gate) _transient[manifest.ComponentId] = ComponentState.Installing;
        try
        {
            if (Directory.Exists(dest))
            {
                if (Directory.Exists(backup))
                    Directory.Delete(backup, true);
                Directory.Move(dest, backup);
            }

            if (string.IsNullOrWhiteSpace(manifest.Sha256))
                throw new InvalidOperationException("HashMissing");

            var installed = await _models.InstallAsync(
                manifest.ComponentId,
                sourceZip,
                manifest.Sha256,
                acceptLicense,
                cancellationToken);

            WriteInstallSidecar(installed.InstallPath, manifest, installed.Sha256);
            var health = _health.Run(installed.InstallPath, manifest.HealthChecks, cancellationToken);
            if (!health.Ok)
            {
                Directory.Delete(installed.InstallPath, true);
                if (Directory.Exists(backup))
                    Directory.Move(backup, dest);
                return new ComponentRecord
                {
                    ComponentId = manifest.ComponentId,
                    InstallPath = dest,
                    State = ComponentState.Broken,
                    Message = "Install health check failed: " + health.Message,
                    Sha256 = installed.Sha256
                };
            }

            if (Directory.Exists(backup))
                Directory.Delete(backup, true);

            return new ComponentRecord
            {
                ComponentId = manifest.ComponentId,
                Version = manifest.PackageVersion,
                InstallPath = installed.InstallPath,
                Sha256 = installed.Sha256,
                LicenseAccepted = true,
                State = ComponentState.Ready,
                Message = "Installed and verified."
            };
        }
        finally
        {
            lock (_gate) _transient.Remove(manifest.ComponentId);
        }
    }

    public Task<ComponentRecord> RepairAsync(ComponentManifest manifest, string sourceZip, CancellationToken cancellationToken = default) =>
        InstallFromZipAsync(manifest, sourceZip, acceptLicense: true, cancellationToken);

    public ComponentHealthReport Verify(ComponentManifest manifest)
    {
        var installPath = Path.Combine(_root, manifest.ComponentId);
        var gate = EvaluateGates(manifest);
        if (gate is not null)
            return new ComponentHealthReport { Ok = false, State = gate.State, Message = gate.Message };
        return _health.Run(installPath, manifest.HealthChecks);
    }

    public void Remove(string componentId)
    {
        _models.Remove(componentId);
        var backup = Path.Combine(_root, componentId + ".bak");
        if (Directory.Exists(backup))
            Directory.Delete(backup, true);
    }

    public bool TryRollback(string componentId)
    {
        ModelManager.ValidatePackageId(componentId);
        var dest = Path.Combine(_root, componentId);
        var backup = dest + ".bak";
        if (!Directory.Exists(backup))
            return false;
        if (Directory.Exists(dest))
            Directory.Delete(dest, true);
        Directory.Move(backup, dest);
        return true;
    }

    public ComponentDiagnostics Diagnose(ComponentManifest manifest)
    {
        var state = GetState(manifest.ComponentId);
        var health = Directory.Exists(state.InstallPath)
            ? _health.Run(state.InstallPath, manifest.HealthChecks)
            : new ComponentHealthReport { Ok = false, State = state.State, Message = state.Message, Details = [] };
        return new ComponentDiagnostics
        {
            ComponentId = manifest.ComponentId,
            State = state.State,
            InstallPath = state.InstallPath,
            Message = state.Message,
            HealthDetails = health.Details,
            Hardware = _hardware()
        };
    }

    private ComponentManifest? FindManifest(string componentId) =>
        _manifests.FirstOrDefault(m => string.Equals(m.ComponentId, componentId, StringComparison.OrdinalIgnoreCase));

    private ComponentRecord? EvaluateGates(ComponentManifest manifest, bool? licenseAccepted = null)
    {
        if (manifest.LicenseAcceptedRequired && licenseAccepted == false)
        {
            return new ComponentRecord
            {
                ComponentId = manifest.ComponentId,
                State = ComponentState.LicenseBlocked,
                Message = "License not accepted."
            };
        }

        if (ReleaseLicenseGate.IsBlockedLicenseId(manifest.LicenseId) ||
            ReleaseLicenseGate.IsBlockedModelId(manifest.ComponentId))
        {
            return new ComponentRecord
            {
                ComponentId = manifest.ComponentId,
                State = ComponentState.LicenseBlocked,
                Message = "License/model is blocked for production use."
            };
        }

        var hw = _hardware();
        if (manifest.Hardware.RequiresCuda && !hw.Cuda)
        {
            return new ComponentRecord
            {
                ComponentId = manifest.ComponentId,
                State = ComponentState.HardwareUnsupported,
                Message = "CUDA required but not detected (nvidia-smi / override)."
            };
        }

        if (manifest.Hardware.MinVramMb > 0 && hw.VramMb > 0 && hw.VramMb < manifest.Hardware.MinVramMb)
        {
            return new ComponentRecord
            {
                ComponentId = manifest.ComponentId,
                State = ComponentState.HardwareUnsupported,
                Message = $"VRAM {hw.VramMb} MB < required {manifest.Hardware.MinVramMb} MB."
            };
        }

        if (manifest.Hardware.MinRamBytes > 0 && hw.RamBytes < manifest.Hardware.MinRamBytes)
        {
            return new ComponentRecord
            {
                ComponentId = manifest.ComponentId,
                State = ComponentState.HardwareUnsupported,
                Message = "Insufficient system RAM."
            };
        }

        if (manifest.Hardware.MinDiskFreeBytes > 0 && hw.DiskFreeBytes < manifest.Hardware.MinDiskFreeBytes)
        {
            return new ComponentRecord
            {
                ComponentId = manifest.ComponentId,
                State = ComponentState.HardwareUnsupported,
                Message = "Insufficient free disk space."
            };
        }

        foreach (var dep in manifest.Dependencies.Where(d => !d.Optional))
        {
            var depState = GetState(dep.ComponentId);
            if (depState.State != ComponentState.Ready)
            {
                return new ComponentRecord
                {
                    ComponentId = manifest.ComponentId,
                    State = ComponentState.NotInstalled,
                    Message = $"Dependency '{dep.ComponentId}' is not Ready ({depState.State})."
                };
            }
        }

        return null;
    }

    private void EnsureManifestTracked(ComponentManifest manifest)
    {
        if (_manifests.All(m => !string.Equals(m.ComponentId, manifest.ComponentId, StringComparison.OrdinalIgnoreCase)))
            _manifests.Add(manifest);
    }

    private static ComponentRecord NotInstalled(string id, string path, ComponentState state, string message) =>
        new()
        {
            ComponentId = id,
            InstallPath = path,
            State = state,
            Message = message
        };

    private static void WriteInstallSidecar(string installPath, ComponentManifest manifest, string sha256)
    {
        var path = Path.Combine(installPath, ".3dgod-component.json");
        File.WriteAllText(path,
            $"{{\"componentId\":\"{manifest.ComponentId}\",\"packageVersion\":\"{manifest.PackageVersion}\",\"sha256\":\"{sha256}\",\"installedUtc\":\"{DateTime.UtcNow:O}\"}}");
    }
}
