using ThreeDGod.Application;

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
                    "Checkpoint/data stays Yellow until audit. No rig will be faked."
            };
        }

        var gated = GatedWorkerCatalog.Probe(WorkerId);
        if (gated.Availability == FeatureAvailability.NotInstalled)
        {
            return new GatedWorkerStatus
            {
                WorkerId = WorkerId,
                Availability = FeatureAvailability.NotInstalled,
                Message =
                    "NotInstalled – SkinTokens checkpoint/runtime is not verified. " +
                    "Code is MIT; weights remain Yellow. No rig will be faked."
            };
        }

        return gated;
    }
}

public sealed class SkinTokensRigService : ISkinTokensRigService
{
    public FeatureAvailability Probe() => SkinTokensRuntime.Probe().Availability;
    public string ProbeMessage() => SkinTokensRuntime.Probe().Message;

    public Task<string> RigGlbAsync(string sourceGlb, string destinationGlb, CancellationToken cancellationToken = default)
    {
        if (File.Exists(destinationGlb))
            File.Delete(destinationGlb);
        var status = SkinTokensRuntime.Probe();
        if (status.Availability is FeatureAvailability.UnsupportedHardware
            or FeatureAvailability.NotInstalled
            or FeatureAvailability.Disabled)
            throw new InvalidOperationException(status.Message);
        throw new InvalidOperationException(
            "NotInstalled – SkinTokens inference is not wired to a verified checkpoint. No rig will be faked.");
    }
}
