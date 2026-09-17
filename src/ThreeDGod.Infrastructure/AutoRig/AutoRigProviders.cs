using ThreeDGod.Application;

namespace ThreeDGod.Infrastructure.AutoRig;

public sealed class SkinTokensCppCpuAutoRigProvider : IAutoRigProvider
{
    public string ProviderId => "skintokens-cpp-cpu";
    public AutoRigProviderKind Kind => AutoRigProviderKind.SkinTokensCppCpu;
    public string DisplayName => "skin-tokens.cpp (CPU)";

    public AutoRigProviderStatus Probe()
    {
        var s = SkinTokensCppRuntime.Probe("cpu");
        return new AutoRigProviderStatus
        {
            ProviderId = ProviderId,
            Kind = Kind,
            Availability = s.Availability,
            Message = s.Message,
            DisplayName = DisplayName
        };
    }

    public async Task<AutoRigResult> RigAsync(string sourceGlb, string destinationGlb, CancellationToken cancellationToken = default)
    {
        var path = await SkinTokensCppRuntime.RigAsync(sourceGlb, destinationGlb, "cpu", cancellationToken: cancellationToken);
        return new AutoRigResult
        {
            OutputGlb = path,
            ProviderId = ProviderId,
            Kind = Kind,
            Device = "cpu",
            Provenance = $"skintokens-cpp@{SkinTokensCppRuntime.UpstreamCommitHint};device=cpu"
        };
    }
}

public sealed class SkinTokensCppVulkanAutoRigProvider : IAutoRigProvider
{
    public string ProviderId => "skintokens-cpp-vulkan";
    public AutoRigProviderKind Kind => AutoRigProviderKind.SkinTokensCppVulkan;
    public string DisplayName => "skin-tokens.cpp (Vulkan)";

    public AutoRigProviderStatus Probe()
    {
        var s = SkinTokensCppRuntime.Probe("vulkan");
        return new AutoRigProviderStatus
        {
            ProviderId = ProviderId,
            Kind = Kind,
            Availability = s.Availability,
            Message = s.Message,
            DisplayName = DisplayName
        };
    }

    public async Task<AutoRigResult> RigAsync(string sourceGlb, string destinationGlb, CancellationToken cancellationToken = default)
    {
        var path = await SkinTokensCppRuntime.RigAsync(sourceGlb, destinationGlb, "vulkan", cancellationToken: cancellationToken);
        return new AutoRigResult
        {
            OutputGlb = path,
            ProviderId = ProviderId,
            Kind = Kind,
            Device = "vulkan",
            Provenance = $"skintokens-cpp@{SkinTokensCppRuntime.UpstreamCommitHint};device=vulkan"
        };
    }
}

public sealed class SkinTokensCudaAutoRigProvider : IAutoRigProvider
{
    private readonly ISkinTokensRigService _cuda;

    public SkinTokensCudaAutoRigProvider(ISkinTokensRigService cuda) => _cuda = cuda;

    public string ProviderId => "skintokens-official-cuda";
    public AutoRigProviderKind Kind => AutoRigProviderKind.SkinTokensOfficialCuda;
    public string DisplayName => "SkinTokens (NVIDIA CUDA)";

    public AutoRigProviderStatus Probe()
    {
        var availability = _cuda.Probe();
        return new AutoRigProviderStatus
        {
            ProviderId = ProviderId,
            Kind = Kind,
            Availability = availability,
            Message = _cuda.ProbeMessage(),
            DisplayName = DisplayName
        };
    }

    public async Task<AutoRigResult> RigAsync(string sourceGlb, string destinationGlb, CancellationToken cancellationToken = default)
    {
        var path = await _cuda.RigGlbAsync(sourceGlb, destinationGlb, cancellationToken);
        return new AutoRigResult
        {
            OutputGlb = path,
            ProviderId = ProviderId,
            Kind = Kind,
            Device = "cuda",
            Provenance = "VAST-AI-Research/SkinTokens;device=cuda"
        };
    }
}
