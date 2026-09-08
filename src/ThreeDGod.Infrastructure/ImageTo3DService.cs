using ThreeDGod.Application;
using ThreeDGod.Core.Domain;

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
    public FeatureAvailability Probe(string backendId = "triposr") => ImageTo3DRuntime.Probe(backendId).Availability;
    public string ProbeMessage(string backendId = "triposr") => ImageTo3DRuntime.Probe(backendId).Message;

    public Task<string> GenerateGlbAsync(string imagePath, string destinationGlb, string backendId = "triposr", CancellationToken cancellationToken = default)
    {
        var status = ImageTo3DRuntime.Probe(backendId);
        if (status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled)
            throw new InvalidOperationException(status.Message);
        throw new InvalidOperationException(
            $"NotInstalled – {backendId} checkpoint/runtime is not verified. No mesh will be generated.");
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
