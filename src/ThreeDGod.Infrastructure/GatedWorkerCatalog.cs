using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Export;
using ThreeDGod.Workers;
using ThreeDGodCreator.Core.Localization;

namespace ThreeDGod.Infrastructure;

public sealed class GatedWorkerStatus
{
    public string WorkerId { get; init; } = "";
    public FeatureAvailability Availability { get; init; }
    public string Message { get; init; } = "";
}

/// <summary>
/// Honest gates for optional heavy workers. Never reports Success or returns a fake mesh.
/// </summary>
public static class GatedWorkerCatalog
{
    public static GatedWorkerStatus Probe(string workerId)
    {
        var envOverride = Environment.GetEnvironmentVariable("3DGOD_WORKER_" + workerId.ToUpperInvariant().Replace('.', '_'));
        if (string.Equals(envOverride, "HardwareUnsupported", StringComparison.OrdinalIgnoreCase))
            return new GatedWorkerStatus { WorkerId = workerId, Availability = FeatureAvailability.UnsupportedHardware, Message = "UnsupportedHardware" };
        if (string.Equals(envOverride, "LicenseBlocked", StringComparison.OrdinalIgnoreCase))
            return new GatedWorkerStatus { WorkerId = workerId, Availability = FeatureAvailability.Disabled, Message = "LicenseBlocked" };

        if (string.Equals(workerId, "anny", StringComparison.OrdinalIgnoreCase))
        {
            var probe = AnnyRuntime.Probe();
            return new GatedWorkerStatus { WorkerId = workerId, Availability = probe.Availability, Message = probe.Message };
        }
        if (string.Equals(workerId, "garmentcode", StringComparison.OrdinalIgnoreCase))
        {
            var probe = GarmentCodeRuntime.Probe();
            return new GatedWorkerStatus { WorkerId = workerId, Availability = probe.Availability, Message = probe.Message };
        }
        if (string.Equals(workerId, "beputhysics", StringComparison.OrdinalIgnoreCase))
        {
            return new GatedWorkerStatus
            {
                WorkerId = workerId,
                Availability = FeatureAvailability.Experimental,
                Message = "Experimental – in-process BepuPhysics accessory preview. Not cloth."
            };
        }
        if (string.Equals(workerId, "flux", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(workerId, "qwen", StringComparison.OrdinalIgnoreCase))
        {
            var probe = ReferenceImageRuntime.Probe(workerId);
            return new GatedWorkerStatus { WorkerId = workerId, Availability = probe.Availability, Message = probe.Message };
        }

        var installDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod", "Models", workerId);
        if (!Directory.Exists(installDir))
            return new GatedWorkerStatus
            {
                WorkerId = workerId,
                Availability = FeatureAvailability.NotInstalled,
                Message = $"NotInstalled – {workerId} is not installed. No mesh will be generated."
            };

        return new GatedWorkerStatus
        {
            WorkerId = workerId,
            Availability = FeatureAvailability.NotInstalled,
            Message = $"NotInstalled – {workerId} package folder exists but no verified runtime/checkpoint."
        };
    }

    public static string[] AllHeavyWorkers { get; } =
    [
        "anny", "triposr", "sf3d", "spar3d", "trellis", "skintokens", "flux", "qwen", "llama", "beputhysics", "garmentcode"
    ];
}

public sealed class LayoutService
{
    public string Save(string json) => json;
    public string ResetCorrupt(string json)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return json;
        }
        catch (System.Text.Json.JsonException)
        {
            return """{"mode":"simple"}""";
        }
    }
}

/// <summary>Backward-compatible bridge to embedded .resx via <see cref="Loc"/>.</summary>
public static class LocalizationCatalog
{
    public static string Get(string locale, string key) => Loc.GetForLocale(locale, key);
}

public sealed class LicenseGate
{
    public bool TryAccept(string profileId, bool userAccepted) =>
        userAccepted &&
        !string.IsNullOrWhiteSpace(profileId) &&
        !ReleaseLicenseGate.IsBlockedLicenseId(profileId);

    public bool TryAcceptModel(string modelId, string licenseId, bool userAccepted) =>
        TryAccept(licenseId, userAccepted) && !ReleaseLicenseGate.IsBlockedModelId(modelId);
}

public static class ExportPreflight
{
    public static IReadOnlyList<string> FbxSanity(string? path, IDiagnosticService? diagnostics = null) =>
        PipelineTrace.Run(diagnostics, "Export", "Export.Preflight", () =>
            (IReadOnlyList<string>)ThreeDGod.Export.FbxSanity.Check(path).Issues, provider: "ue5-preflight");

    public static Ue5PreflightReport EvaluateGlbForUe5(string glbPath, string? assetName = null, bool requireSkin = true, IDiagnosticService? diagnostics = null) =>
        UnrealEngine5ExportProfile.EvaluateGlb(glbPath, assetName, requireSkin, diagnostics);
}
