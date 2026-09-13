using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Workers;

namespace ThreeDGod.Infrastructure;

public static class SkinTokensRuntime
{
    public const int MinVramMb = 14000;
    public const string WorkerId = "skintokens";

    public static GatedWorkerStatus Probe()
    {
        var hw = HardwareProfiler.Probe();
        if (!hw.Cuda || hw.VramMb < MinVramMb)
        {
            return new GatedWorkerStatus
            {
                WorkerId = WorkerId,
                Availability = FeatureAvailability.UnsupportedHardware,
                Message =
                    "UnsupportedHardware – SkinTokens needs NVIDIA CUDA and >=14GB VRAM. " +
                    "IMPLEMENTED_GATED_HARDWARE (MIT code+weights). No rig will be faked."
            };
        }

        var root = AnnyRuntime.FindRepoRoot();
        var projectDir = Path.Combine(root, "workers", "skintokens");
        var script = Path.Combine(projectDir, "skintokens_worker.py");
        var lockFile = Path.Combine(projectDir, "uv.lock");
        if (!File.Exists(script) || (!File.Exists(lockFile) && !Directory.Exists(Path.Combine(projectDir, ".venv"))))
        {
            return new GatedWorkerStatus
            {
                WorkerId = WorkerId,
                Availability = FeatureAvailability.NotInstalled,
                Message =
                    "NotInstalled – SkinTokens worker/uv env missing. " +
                    "MIT (VAST-AI-Research/SkinTokens + VAST-AI/SkinTokens). No rig will be faked."
            };
        }

        var modelDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod", "Models", "skintokens");
        var hasCkpt =
            File.Exists(Path.Combine(modelDir, "experiments", "articulation_xl_quantization_256_token_4", "grpo_1400.ckpt"))
            && File.Exists(Path.Combine(modelDir, "experiments", "skin_vae_2_10_32768", "last.ckpt"));
        if (!hasCkpt)
        {
            return new GatedWorkerStatus
            {
                WorkerId = WorkerId,
                Availability = FeatureAvailability.NotInstalled,
                Message =
                    "NotInstalled – SkinTokens MIT checkpoints missing. Acquire via Setup Assistant. " +
                    "IMPLEMENTED_GATED_HARDWARE until CUDA+checkpoints+upstream PASS_REAL. No rig will be faked."
            };
        }

        return new GatedWorkerStatus
        {
            WorkerId = WorkerId,
            Availability = FeatureAvailability.Experimental,
            Message = "Experimental – SkinTokens worker ready (MIT). Real skinned GLB only via CUDA inference."
        };
    }
}

public sealed class SkinTokensRigService : ISkinTokensRigService
{
    private readonly IDiagnosticService? _diagnostics;

    public SkinTokensRigService(IDiagnosticService? diagnostics = null) => _diagnostics = diagnostics;

    public FeatureAvailability Probe() => SkinTokensRuntime.Probe().Availability;
    public string ProbeMessage() => SkinTokensRuntime.Probe().Message;

    public async Task<string> RigGlbAsync(string sourceGlb, string destinationGlb, CancellationToken cancellationToken = default)
    {
        if (File.Exists(destinationGlb))
            File.Delete(destinationGlb);

        return await PipelineTrace.RunAsync(_diagnostics, "Rig", "SkinTokens.Rig", async () =>
        {
            var status = SkinTokensRuntime.Probe();
            if (status.Availability is FeatureAvailability.UnsupportedHardware
                or FeatureAvailability.NotInstalled
                or FeatureAvailability.Disabled)
                throw new InvalidOperationException(status.Message);

            var root = AnnyRuntime.FindRepoRoot();
            var projectDir = Path.Combine(root, "workers", "skintokens");
            var script = Path.Combine(projectDir, "skintokens_worker.py");
            var uv = AnnyRuntime.FindUv()
                     ?? throw new InvalidOperationException("NotInstalled – uv missing for SkinTokens worker.");

            await using var session = await WorkerSession.StartAsync(
                uv,
                ["run", "--project", projectDir, "python", "-u", script],
                TimeSpan.FromMinutes(5),
                cancellationToken: cancellationToken);

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                inputGlb = Path.GetFullPath(sourceGlb),
                outputGlb = Path.GetFullPath(destinationGlb),
                modelDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "3DGod", "Models", "skintokens")
            });
            var result = await session.RequestAsync(
                new WorkerRequest("mesh.autoroot.rig", payload, JobId: Guid.NewGuid().ToString("N"), BackendId: "skintokens"),
                TimeSpan.FromHours(2),
                cancellationToken);
            if (!result.Ok)
                throw new InvalidOperationException(result.ErrorMessage ?? result.ErrorCode ?? "SkinTokens worker failed.");
            if (!File.Exists(destinationGlb) || new FileInfo(destinationGlb).Length < 64)
                throw new InvalidOperationException("SkinTokens worker did not write a valid skinned GLB. No rig will be faked.");
            return destinationGlb;
        }, provider: "skintokens").ConfigureAwait(false);
    }
}
