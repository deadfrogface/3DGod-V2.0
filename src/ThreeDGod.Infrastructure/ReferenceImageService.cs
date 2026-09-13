using System.Security.Cryptography;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Domain;
using ThreeDGod.Workers;

namespace ThreeDGod.Infrastructure;

public sealed class ReferenceImageRuntimeStatus
{
    public FeatureAvailability Availability { get; init; } = FeatureAvailability.NotInstalled;
    public string Message { get; init; } = "";
    public string? BackendId { get; init; }
    public string? CheckpointPath { get; init; }
}

public static class ReferenceImageRuntime
{
    public static readonly string[] Backends = ["flux", "qwen"];

    public static ReferenceImageRuntimeStatus Probe(string? backendId = null)
    {
        if (string.IsNullOrWhiteSpace(backendId) ||
            string.Equals(backendId, "flux", StringComparison.OrdinalIgnoreCase))
        {
            var flux = FluxRuntime.Probe();
            if (flux.Availability is not FeatureAvailability.NotInstalled || flux.HasCheckpoint)
            {
                return new ReferenceImageRuntimeStatus
                {
                    Availability = flux.Availability,
                    Message = flux.Message,
                    BackendId = "flux",
                    CheckpointPath = flux.ModelDir
                };
            }
        }

        var hw = HardwareProfiler.Probe();
        var candidates = string.IsNullOrWhiteSpace(backendId) ? Backends : [backendId];
        ReferenceImageRuntimeStatus? firstCheckpoint = null;
        foreach (var id in candidates)
        {
            if (string.Equals(id, "flux", StringComparison.OrdinalIgnoreCase))
                continue;
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Models", id);
            if (!Directory.Exists(dir))
                continue;
            var checkpoint = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                .FirstOrDefault(p =>
                {
                    var ext = Path.GetExtension(p).ToLowerInvariant();
                    return ext is ".safetensors" or ".gguf" or ".ckpt" or ".pt" && new FileInfo(p).Length > 1024 * 1024;
                });
            if (checkpoint is null)
                continue;
            firstCheckpoint = new ReferenceImageRuntimeStatus
            {
                Availability = hw.Cuda && hw.VramMb >= 8192
                    ? FeatureAvailability.Experimental
                    : FeatureAvailability.UnsupportedHardware,
                Message = hw.Cuda && hw.VramMb >= 8192
                    ? $"Experimental – {id} checkpoint found ({Path.GetFileName(checkpoint)})."
                    : $"UnsupportedHardware – {id} checkpoint found but CUDA/VRAM is insufficient. No image will be faked.",
                BackendId = id,
                CheckpointPath = checkpoint
            };
            if (firstCheckpoint.Availability == FeatureAvailability.Experimental)
                return firstCheckpoint;
        }

        if (firstCheckpoint is not null)
            return firstCheckpoint;

        var fluxStatus = FluxRuntime.Probe();
        return new ReferenceImageRuntimeStatus
        {
            Availability = fluxStatus.Availability,
            Message = fluxStatus.Message,
            BackendId = "flux",
            CheckpointPath = fluxStatus.ModelDir
        };
    }
}

public sealed class ReferenceImageService : IReferenceImageGenerationService
{
    private readonly IDiagnosticService? _diagnostics;

    public ReferenceImageService(IDiagnosticService? diagnostics = null) => _diagnostics = diagnostics;

    public FeatureAvailability Probe() => ReferenceImageRuntime.Probe().Availability;
    public string ProbeMessage() => ReferenceImageRuntime.Probe().Message;

    public Task<ReferenceImage> GenerateAsync(string prompt, long? seed, ProjectBundle bundle, CancellationToken cancellationToken = default)
    {
        var status = ReferenceImageRuntime.Probe("flux");
        return PipelineTrace.RunAsync(_diagnostics, "AI", "ReferenceImage.Generate", async () =>
        {
            if (status.Availability is FeatureAvailability.NotInstalled
                or FeatureAvailability.UnsupportedHardware
                or FeatureAvailability.Disabled)
                throw new InvalidOperationException(status.Message);

            var work = Path.Combine(Path.GetTempPath(), "3dgod-flux-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            var pngPath = Path.Combine(work, "flux.png");
            try
            {
                await using var flux = new FluxService(new WorkerProcessHost(_diagnostics), _diagnostics);
                await flux.GeneratePngAsync(prompt, pngPath, seed, cancellationToken: cancellationToken).ConfigureAwait(false);
                var image = AttachExistingPng(pngPath, prompt, seed, bundle);
                image.BackendId = "flux";
                image.ModelId = FluxRuntime.HfRepo;
                image.Source = "flux-schnell";
                return image;
            }
            finally
            {
                try { Directory.Delete(work, recursive: true); } catch { /* best-effort */ }
            }
        }, provider: status.BackendId ?? "flux");
    }

public ReferenceImage AttachExistingPng(string pngPath, string prompt, long? seed, ProjectBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        if (string.IsNullOrWhiteSpace(pngPath) || !File.Exists(pngPath))
            throw new InvalidOperationException("Reference PNG not found.");
        var bytes = File.ReadAllBytes(pngPath);
        if (!PngSignature.TryReadSize(bytes, out var width, out var height))
            throw new InvalidOperationException("File is not a valid PNG. A placeholder is not accepted.");

        var set = bundle.ReferenceSets.FirstOrDefault() ?? new ReferenceSet { Name = "default" };
        if (bundle.ReferenceSets.Count == 0)
            bundle.ReferenceSets.Add(set);

        var image = new ReferenceImage
        {
            ReferenceSetId = set.ReferenceSetId,
            Prompt = prompt ?? "",
            Seed = seed,
            BackendId = "import",
            ModelId = "",
            ModelHash = "",
            Width = width,
            Height = height,
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            RelativePath = $"references/{Guid.NewGuid():D}/image.png",
            Source = "imported-png"
        };
        image.RelativePath = $"references/{image.ReferenceImageId:D}/image.png";
        set.ImageIds.Add(image.ReferenceImageId);
        bundle.ReferenceImages.Add(image);
        bundle.ReferenceImageBytes[image.ReferenceImageId] = bytes;
        return image;
    }
}

public static class PngSignature
{
    private static readonly byte[] Magic = [137, 80, 78, 71, 13, 10, 26, 10];

    public static bool TryReadSize(ReadOnlySpan<byte> png, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (png.Length < 24)
            return false;
        if (!png[..8].SequenceEqual(Magic))
            return false;
        width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return width > 0 && height > 0;
    }
}
