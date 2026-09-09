using System.Text.Json;

namespace ThreeDGod.Infrastructure;

public sealed class ModelLicenseRecord
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "";
    public string LicenseId { get; init; } = "";
    public string? LicenseRef { get; init; }
    public string ShippingStatus { get; init; } = "";
    public bool CommercialUse { get; init; }
    public bool RequiresUserAccept { get; init; }
    public bool Verified { get; init; }
}

public sealed class ModelLicenseRegistry
{
    public int SchemaVersion { get; init; }
    public string LastUpdated { get; init; } = "";
    public List<ModelLicenseRecord> Components { get; init; } = [];
    public ModelLicenseBlocklist Blocklist { get; init; } = new();
}

public sealed class ModelLicenseBlocklist
{
    public List<string> ModelIds { get; init; } = [];
    public List<string> LicenseIds { get; init; } = [];
}

public sealed class ReleaseLicenseValidationResult
{
    public bool Ok { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

public static class ReleaseLicenseGate
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static readonly HashSet<string> BuiltInBlockedModelIds =
    [
        "smpl-x", "smplx", "hunyuan-eu", "hunyuan3d-eu", "hunyuan3d", "anigen", "anigen-restricted"
    ];

    public static readonly HashSet<string> BuiltInBlockedLicenseIds =
    [
        "unknown", "unverified", "s-lab-nc", "cc-by-nc-4.0", "gpl-3.0-linked", "smpl-x-noncommercial", "hunyuan-eu-restricted"
    ];

    public static bool IsBlockedModelId(string id) =>
        !string.IsNullOrWhiteSpace(id) &&
        BuiltInBlockedModelIds.Contains(id.Trim().ToLowerInvariant());

    public static bool IsBlockedLicenseId(string licenseId) =>
        !string.IsNullOrWhiteSpace(licenseId) &&
        BuiltInBlockedLicenseIds.Contains(licenseId.Trim().ToLowerInvariant());

    public static ModelLicenseRegistry LoadRegistry(string repoRoot)
    {
        var path = Path.Combine(repoRoot, "MODEL_LICENSES.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("MODEL_LICENSES.json is required for release.", path);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ModelLicenseRegistry>(json, JsonOptions)
            ?? throw new InvalidOperationException("MODEL_LICENSES.json parse failed.");
    }

    public static ReleaseLicenseValidationResult ValidateForRelease(string repoRoot)
    {
        var errors = new List<string>();
        var registry = LoadRegistry(repoRoot);

        if (registry.SchemaVersion != 1)
            errors.Add($"MODEL_LICENSES schemaVersion must be 1 (got {registry.SchemaVersion})");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in registry.Components)
        {
            if (string.IsNullOrWhiteSpace(component.Id))
            {
                errors.Add("Component with empty id");
                continue;
            }
            if (!ids.Add(component.Id))
                errors.Add($"Duplicate component id: {component.Id}");
            if (string.IsNullOrWhiteSpace(component.LicenseId))
                errors.Add($"Component '{component.Id}' missing licenseId");
            if (IsBlockedModelId(component.Id))
                errors.Add($"Blocklisted model id in registry: {component.Id}");
            if (IsBlockedLicenseId(component.LicenseId))
                errors.Add($"Blocklisted license id for component '{component.Id}': {component.LicenseId}");
            if (component.ShippingStatus is "production" or "available")
            {
                if (!component.CommercialUse)
                    errors.Add($"Production component '{component.Id}' must allow commercial use");
                if (!component.Verified)
                    errors.Add($"Production component '{component.Id}' must be verified");
            }
        }

        foreach (var blocked in registry.Blocklist.ModelIds.Concat(BuiltInBlockedModelIds))
        {
            if (registry.Components.Any(c => string.Equals(c.Id, blocked, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"Blocklisted id must not appear as shipping component: {blocked}");
        }

        var noticesPath = Path.Combine(repoRoot, "THIRD_PARTY_NOTICES.txt");
        if (!File.Exists(noticesPath))
            errors.Add("THIRD_PARTY_NOTICES.txt is missing");

        foreach (var manifestPath in WorkerPackageManifestReader.DiscoverManifestPaths(repoRoot))
        {
            var manifest = WorkerPackageManifestReader.Load(manifestPath);
            var manifestResult = WorkerPackageManifestReader.Validate(manifest, repoRoot);
            foreach (var err in manifestResult.Errors)
                errors.Add($"{Path.GetFileName(manifestPath)}: {err}");

            if (!registry.Components.Any(c => string.Equals(c.Id, manifest.WorkerId, StringComparison.OrdinalIgnoreCase)))
                errors.Add($"Worker '{manifest.WorkerId}' has manifest but no MODEL_LICENSES record");
        }

        return new ReleaseLicenseValidationResult { Ok = errors.Count == 0, Errors = errors };
    }
}
