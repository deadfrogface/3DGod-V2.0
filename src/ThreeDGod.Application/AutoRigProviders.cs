namespace ThreeDGod.Application;

/// <summary>Multi-backend Auto-Rig providers (CPU / Vulkan / CUDA). Freeform distance weights stay separate.</summary>
public enum AutoRigProviderKind
{
    Automatic = 0,
    SkinTokensCppCpu = 1,
    SkinTokensCppVulkan = 2,
    SkinTokensOfficialCuda = 3
}

public enum AutoRigDevicePreference
{
    Automatic = 0,
    Cpu = 1,
    Vulkan = 2,
    NvidiaCuda = 3
}

public sealed class AutoRigProviderStatus
{
    public required string ProviderId { get; init; }
    public required AutoRigProviderKind Kind { get; init; }
    public required FeatureAvailability Availability { get; init; }
    public required string Message { get; init; }
    public string? DisplayName { get; init; }
}

public sealed class AutoRigSelection
{
    public AutoRigProviderStatus? Selected { get; init; }
    public required string Reason { get; init; }
    public IReadOnlyList<AutoRigProviderStatus> Candidates { get; init; } = [];
    public IReadOnlyList<(string ProviderId, string UnavailableReason)> Unavailable { get; init; } = [];
}

public sealed class AutoRigResult
{
    public required string OutputGlb { get; init; }
    public required string ProviderId { get; init; }
    public required AutoRigProviderKind Kind { get; init; }
    public required string Device { get; init; }
    public string? Provenance { get; init; }
}

public interface IAutoRigProvider
{
    string ProviderId { get; }
    AutoRigProviderKind Kind { get; }
    string DisplayName { get; }
    AutoRigProviderStatus Probe();
    Task<AutoRigResult> RigAsync(string sourceGlb, string destinationGlb, CancellationToken cancellationToken = default);
}

public interface IAutoRigProviderSelector
{
    AutoRigSelection Select(AutoRigDevicePreference preference = AutoRigDevicePreference.Automatic);
    IReadOnlyList<AutoRigProviderStatus> ListProviders();
    Task<AutoRigResult> RigAsync(
        string sourceGlb,
        string destinationGlb,
        AutoRigDevicePreference preference = AutoRigDevicePreference.Automatic,
        CancellationToken cancellationToken = default);
}

public interface IAutoRigService
{
    FeatureAvailability Probe();
    string ProbeMessage();
    AutoRigSelection DescribeSelection(AutoRigDevicePreference preference = AutoRigDevicePreference.Automatic);
    Task<AutoRigResult> RigAsync(
        string sourceGlb,
        string destinationGlb,
        AutoRigDevicePreference preference = AutoRigDevicePreference.Automatic,
        CancellationToken cancellationToken = default);
}
