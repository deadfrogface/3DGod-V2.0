using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Workers;

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

public static class LocalizationCatalog
{
    private static readonly Dictionary<string, Dictionary<string, string>> Catalog = new()
    {
        ["de"] = new() { ["app.title"] = "3D God", ["action.undo"] = "Rückgängig" },
        ["en"] = new() { ["app.title"] = "3D God", ["action.undo"] = "Undo" }
    };

    public static string Get(string locale, string key) =>
        Catalog.TryGetValue(locale, out var map) && map.TryGetValue(key, out var value) ? value : key;
}

public sealed class LicenseGate
{
    public bool TryAccept(string profileId, bool userAccepted) =>
        userAccepted && !string.IsNullOrWhiteSpace(profileId);
}

public static class ExportPreflight
{
    public static IReadOnlyList<string> FbxSanity(string? path, IDiagnosticService? diagnostics = null)
    {
        return PipelineTrace.Run(diagnostics, "Export", "Export.Preflight", () =>
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                issues.Add("FBX missing – real UE5 import is not claimed.");
            else
            {
                var len = new FileInfo(path).Length;
                if (len < 64)
                    issues.Add("FBX too small to be a valid scene.");
            }
            return (IReadOnlyList<string>)issues;
        }, provider: "ue5-preflight");
    }
}
