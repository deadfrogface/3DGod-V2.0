namespace ThreeDGod.Infrastructure.Components;

/// <summary>Product-facing Setup Assistant features (users never need backend names).</summary>
public enum SetupFeatureId
{
    HumanCreator,
    ImageTo3D,
    TextToCharacter,
    ParametricClothing,
    AutomaticRigging,
    Advanced3DQuality,
    UnrealExport
}

public sealed class SetupFeatureDescriptor
{
    public SetupFeatureId FeatureId { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string? PrimaryComponentId { get; init; }
    public string? AdvancedBackendName { get; init; }
    public bool Optional { get; init; } = true;
}

public sealed class SetupFeatureStatus
{
    public SetupFeatureDescriptor Feature { get; init; } = new();
    public ComponentState State { get; init; } = ComponentState.Unknown;
    public string Message { get; init; } = "";
    public bool CanInstall { get; init; }
    public bool CanRepair { get; init; }
    public bool CanRemove { get; init; }
}

public static class SetupAssistantCatalog
{
    /// <summary>
    /// Providers that must never offer Setup Assistant install until an explicit product decision reverses the reject/gate.
    /// </summary>
    private static readonly HashSet<string> InstallRejectedIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "sf3d",
        "spar3d",
        "skintokens"
    };

    public static IReadOnlyList<SetupFeatureDescriptor> Features { get; } =
    [
        new()
        {
            FeatureId = SetupFeatureId.HumanCreator,
            Title = "Human Creator",
            Description = "Create realistic humans with local body parameters.",
            PrimaryComponentId = "anny",
            AdvancedBackendName = "Anny",
            Optional = true
        },
        new()
        {
            FeatureId = SetupFeatureId.ImageTo3D,
            Title = "Image → 3D",
            Description = "Turn a reference image into a 3D mesh.",
            PrimaryComponentId = "triposr",
            AdvancedBackendName = "TripoSR",
            Optional = true
        },
        new()
        {
            FeatureId = SetupFeatureId.TextToCharacter,
            Title = "Text → Character",
            Description = "Generate a reference image from text, then build a character.",
            PrimaryComponentId = "flux",
            AdvancedBackendName = "FLUX.1-schnell",
            Optional = true
        },
        new()
        {
            FeatureId = SetupFeatureId.ParametricClothing,
            Title = "Parametric Clothing",
            Description = "Build garment patterns and meshes from measurements.",
            PrimaryComponentId = "garmentcode",
            AdvancedBackendName = "GarmentCode",
            Optional = true
        },
        new()
        {
            FeatureId = SetupFeatureId.AutomaticRigging,
            Title = "Automatic Rigging",
            Description = "Add a skeleton and skin weights to a mesh.",
            PrimaryComponentId = "skintokens",
            AdvancedBackendName = "SkinTokens",
            Optional = true
        },
        new()
        {
            FeatureId = SetupFeatureId.Advanced3DQuality,
            Title = "Advanced 3D Quality",
            Description = "SF3D/SPAR3D rejected for this cycle — license/hardware/product win not cleared vs TripoSR.",
            PrimaryComponentId = "sf3d",
            AdvancedBackendName = "SF3D / SPAR3D (REJECTED)",
            Optional = true
        },
        new()
        {
            FeatureId = SetupFeatureId.UnrealExport,
            Title = "Unreal Export",
            Description = "Prepare assets for Unreal Engine 5 (Blender headless FBX path).",
            PrimaryComponentId = "blender",
            AdvancedBackendName = "Blender",
            Optional = true
        }
    ];

    public static IReadOnlyList<SetupFeatureStatus> Snapshot(IComponentManager components)
    {
        var manifests = components.ListManifests().ToDictionary(m => m.ComponentId, StringComparer.OrdinalIgnoreCase);
        return Features.Select(feature =>
        {
            if (string.IsNullOrWhiteSpace(feature.PrimaryComponentId))
            {
                return new SetupFeatureStatus
                {
                    Feature = feature,
                    State = ComponentState.Unknown,
                    Message = "No component mapping."
                };
            }

            var id = feature.PrimaryComponentId;
            if (InstallRejectedIds.Contains(id))
            {
                var rejectedState = manifests.TryGetValue(id, out _)
                    ? components.GetState(id)
                    : new ComponentRecord
                    {
                        ComponentId = id,
                        State = ComponentState.DownloadUnavailable,
                        Message = "Rejected for this cycle."
                    };
                return new SetupFeatureStatus
                {
                    Feature = feature,
                    State = rejectedState.State is ComponentState.Ready
                        ? ComponentState.DownloadUnavailable
                        : rejectedState.State,
                    Message = id is "skintokens"
                        ? "GATED_LICENSE / GATED_HARDWARE – SkinTokens is not integrated until license + VRAM are cleared."
                        : "REJECTED – SF3D/SPAR3D not selected for product install this cycle (see docs/audit/STAGE13_SF3D_SPAR3D_DECISION.md).",
                    CanInstall = false,
                    CanRepair = false,
                    CanRemove = rejectedState.State is ComponentState.Ready or ComponentState.Broken
                };
            }

            if (!manifests.TryGetValue(id, out var manifest) &&
                id is "triposr" or "flux" or "sf3d" or "skintokens" or "blender")
            {
                return new SetupFeatureStatus
                {
                    Feature = feature,
                    State = ComponentState.DownloadUnavailable,
                    Message = "Optional provider is not packaged for end-user install yet.",
                    CanInstall = false
                };
            }

            var state = components.GetState(id);
            return new SetupFeatureStatus
            {
                Feature = feature,
                State = state.State,
                Message = state.Message,
                CanInstall = CanOfferInstall(manifest, state.State),
                CanRepair = state.State is ComponentState.Ready or ComponentState.Broken or ComponentState.UpdateAvailable,
                CanRemove = state.State is ComponentState.Ready or ComponentState.Broken or ComponentState.UpdateAvailable
            };
        }).ToList();
    }

    /// <summary>
    /// Install is offered only when a real local source or download URL exists.
    /// DownloadUnavailable / LicenseBlocked / HardwareUnsupported never claim installability.
    /// </summary>
    public static bool CanOfferInstall(ComponentManifest? manifest, ComponentState state)
    {
        if (state is ComponentState.DownloadUnavailable
            or ComponentState.LicenseBlocked
            or ComponentState.HardwareUnsupported
            or ComponentState.Ready
            or ComponentState.Installing
            or ComponentState.Unknown)
            return false;

        if (state is not (ComponentState.NotInstalled or ComponentState.Optional or ComponentState.Broken or ComponentState.UpdateAvailable))
            return false;

        if (manifest is null)
            return false;

        if (InstallRejectedIds.Contains(manifest.ComponentId))
            return false;

        return !string.IsNullOrWhiteSpace(manifest.DownloadUrl)
               || !string.IsNullOrWhiteSpace(manifest.LocalSourceHint);
    }
}
