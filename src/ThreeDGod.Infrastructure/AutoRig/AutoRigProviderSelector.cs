using ThreeDGod.Application;

namespace ThreeDGod.Infrastructure.AutoRig;

/// <summary>
/// Hardware-aware Auto-Rig selection. Never reports Vulkan PASS when falling back to CPU.
/// </summary>
public sealed class AutoRigProviderSelector : IAutoRigProviderSelector
{
    private readonly IReadOnlyList<IAutoRigProvider> _providers;

    public AutoRigProviderSelector(IEnumerable<IAutoRigProvider> providers) =>
        _providers = providers.ToList();

    public IReadOnlyList<AutoRigProviderStatus> ListProviders() =>
        _providers.Select(p => p.Probe()).ToList();

    public AutoRigSelection Select(AutoRigDevicePreference preference = AutoRigDevicePreference.Automatic)
    {
        var statuses = _providers.Select(p => (Provider: p, Status: p.Probe())).ToList();
        var unavailable = statuses
            .Where(x => x.Status.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
            .Select(x => (x.Status.ProviderId, x.Status.Message))
            .ToList();

        IAutoRigProvider? pick = preference switch
        {
            AutoRigDevicePreference.Cpu =>
                FirstInvocable(statuses, AutoRigProviderKind.SkinTokensCppCpu),
            AutoRigDevicePreference.Vulkan =>
                FirstInvocable(statuses, AutoRigProviderKind.SkinTokensCppVulkan),
            AutoRigDevicePreference.NvidiaCuda =>
                FirstInvocable(statuses, AutoRigProviderKind.SkinTokensOfficialCuda),
            _ =>
                FirstInvocable(statuses, AutoRigProviderKind.SkinTokensCppVulkan)
                ?? FirstInvocable(statuses, AutoRigProviderKind.SkinTokensCppCpu)
                ?? FirstInvocable(statuses, AutoRigProviderKind.SkinTokensOfficialCuda)
        };

        if (pick is null)
        {
            return new AutoRigSelection
            {
                Selected = null,
                Reason = "No Auto-Rig provider is installed and supported on this hardware.",
                Candidates = statuses.Select(x => x.Status).ToList(),
                Unavailable = unavailable
            };
        }

        var selected = statuses.First(x => ReferenceEquals(x.Provider, pick)).Status;
        var reason = preference == AutoRigDevicePreference.Automatic
            ? $"Automatic selected {selected.DisplayName ?? selected.ProviderId}: {selected.Message}"
            : $"Preference {preference} selected {selected.DisplayName ?? selected.ProviderId}: {selected.Message}";

        return new AutoRigSelection
        {
            Selected = selected,
            Reason = reason,
            Candidates = statuses.Select(x => x.Status).ToList(),
            Unavailable = unavailable
        };
    }

    public async Task<AutoRigResult> RigAsync(
        string sourceGlb,
        string destinationGlb,
        AutoRigDevicePreference preference = AutoRigDevicePreference.Automatic,
        CancellationToken cancellationToken = default)
    {
        var selection = Select(preference);
        if (selection.Selected is null)
        {
            if (preference == AutoRigDevicePreference.Vulkan)
            {
                throw new InvalidOperationException(
                    "Vulkan Auto-Rig requested but Vulkan provider is unavailable. " +
                    "Not falling back to CPU while claiming Vulkan. " + selection.Reason);
            }
            throw new InvalidOperationException(selection.Reason);
        }

        var provider = _providers.First(p => p.ProviderId == selection.Selected.ProviderId);
        // Honesty: if user asked Vulkan but we somehow have a different pick, refuse silent CPU claim.
        if (preference == AutoRigDevicePreference.Vulkan
            && provider.Kind != AutoRigProviderKind.SkinTokensCppVulkan)
        {
            throw new InvalidOperationException(
                "Vulkan Auto-Rig requested but Vulkan provider is unavailable. " +
                "Not falling back to CPU while claiming Vulkan. " + selection.Reason);
        }

        return await provider.RigAsync(sourceGlb, destinationGlb, cancellationToken);
    }

    private static IAutoRigProvider? FirstInvocable(
        List<(IAutoRigProvider Provider, AutoRigProviderStatus Status)> statuses,
        AutoRigProviderKind kind)
    {
        var hit = statuses.FirstOrDefault(x =>
            x.Provider.Kind == kind
            && x.Status.Availability is FeatureAvailability.Available or FeatureAvailability.Experimental);
        return hit.Provider;
    }
}

public sealed class AutoRigService : IAutoRigService, IAutoRigBackend, IRiggingService
{
    private readonly IAutoRigProviderSelector _selector;

    public AutoRigService(IAutoRigProviderSelector selector) => _selector = selector;

    public string BackendId => "autoroot-multi";

    public FeatureAvailability Probe()
    {
        var sel = _selector.Select();
        return sel.Selected?.Availability ?? FeatureAvailability.NotInstalled;
    }

    public string ProbeMessage()
    {
        var sel = _selector.Select();
        if (sel.Selected is null)
            return "Auto-rig backend is not installed. " + sel.Reason
                   + " Freeform distance weights and authored humanoid test rigs remain available.";
        return sel.Reason;
    }

    public AutoRigSelection DescribeSelection(AutoRigDevicePreference preference = AutoRigDevicePreference.Automatic) =>
        _selector.Select(preference);

    public Task<AutoRigResult> RigAsync(
        string sourceGlb,
        string destinationGlb,
        AutoRigDevicePreference preference = AutoRigDevicePreference.Automatic,
        CancellationToken cancellationToken = default) =>
        _selector.RigAsync(sourceGlb, destinationGlb, preference, cancellationToken);
}
