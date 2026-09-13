using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Domain;
using ThreeDGod.Workers;

namespace ThreeDGod.Infrastructure;

public static class ImageTo3DRuntime
{
    public static readonly string[] Backends = ["triposr", "sf3d", "spar3d", "trellis"];

    public static GatedWorkerStatus Probe(string backendId = "triposr")
    {
        if (string.IsNullOrWhiteSpace(backendId))
            backendId = "triposr";
        if (backendId is "sf3d" or "spar3d" && !StabilityLicense.IsAccepted(backendId))
            return new GatedWorkerStatus
            {
                WorkerId = backendId,
                Availability = FeatureAvailability.Disabled,
                Message = $"LicenseBlocked – {backendId} requires accepted Stability Community License + attribution. No mesh will be generated."
            };

        var gated = GatedWorkerCatalog.Probe(backendId);
        var hw = HardwareProfiler.Probe();
        if (gated.Availability == FeatureAvailability.NotInstalled)
            return gated;
        if (ImageTo3DProfiles.Select(backendId, hw.VramMb, hw.Cuda) is null)
            return new GatedWorkerStatus
            {
                WorkerId = backendId,
                Availability = FeatureAvailability.UnsupportedHardware,
                Message = $"UnsupportedHardware – no {backendId} profile fits this machine. No mesh will be faked."
            };
        return gated;
    }
}

public sealed class ImageTo3DService : IImageTo3DService
{
    private readonly IDiagnosticService? _diagnostics;

    public ImageTo3DService(IDiagnosticService? diagnostics = null) => _diagnostics = diagnostics;

    public FeatureAvailability Probe(string backendId = "triposr") => ImageTo3DRuntime.Probe(backendId).Availability;
    public string ProbeMessage(string backendId = "triposr") => ImageTo3DRuntime.Probe(backendId).Message;

    public Task<string> GenerateGlbAsync(string imagePath, string destinationGlb, string backendId = "triposr", CancellationToken cancellationToken = default)
    {
        var status = ImageTo3DRuntime.Probe(backendId);
        if (status.Availability is FeatureAvailability.NotInstalled
            or FeatureAvailability.UnsupportedHardware
            or FeatureAvailability.Disabled)
        {
            return PipelineTrace.RunAsync<string>(_diagnostics, "AI", "ImageTo3D.Generate", () =>
                Task.FromException<string>(new InvalidOperationException(status.Message)), provider: backendId);
        }

        if (string.Equals(backendId, "triposr", StringComparison.OrdinalIgnoreCase))
        {
            return PipelineTrace.RunAsync(_diagnostics, "AI", "ImageTo3D.Generate", async () =>
            {
                await using var triposr = new TripoSrService(new WorkerProcessHost(_diagnostics), _diagnostics);
                return await triposr.GenerateGlbAsync(imagePath, destinationGlb, cancellationToken).ConfigureAwait(false);
            }, provider: backendId);
        }

        return PipelineTrace.RunAsync<string>(_diagnostics, "AI", "ImageTo3D.Generate", () =>
            Task.FromException<string>(new InvalidOperationException(
                $"NotInstalled – {backendId} checkpoint/runtime is not verified. No mesh will be generated.")),
            provider: backendId);
    }

    public MeshAsset AttachExistingGlb(string glbPath, ProjectBundle bundle, string backendId = "import")
    {
        if (!File.Exists(glbPath))
            throw new InvalidOperationException("GLB not found.");
        var ext = Path.GetExtension(glbPath);
        if (!string.Equals(ext, ".glb", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only a real GLB can be attached.");
        var info = new FileInfo(glbPath);
        if (info.Length < 64)
            throw new InvalidOperationException("GLB is too small to be a mesh.");
        var asset = new MeshAsset
        {
            Name = Path.GetFileNameWithoutExtension(glbPath),
            SourceFormat = "glb",
            CanonicalGlbPath = Path.GetFullPath(glbPath),
            ValidationState = "imported"
        };
        bundle.Meshes.Add(asset);
        bundle.Project.AssetIds.Add(asset.MeshAssetId);
        return asset;
    }
}
