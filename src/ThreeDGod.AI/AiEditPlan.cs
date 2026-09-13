using System.Text.Json;

namespace ThreeDGod.AI;

public sealed class AiEditPlan
{
    public string SchemaVersion { get; init; } = AiEditPlanSchema.Version;
    public string Status { get; init; } = "Unsupported";
    public string? Operation { get; init; }
    public Dictionary<string, string> Args { get; init; } = [];
    public string? Provider { get; init; }
    public string? Reason { get; init; }
}

public static class AiEditPlanSchema
{
    public const string Version = "3dgod-ai-edit/1";

    public static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "valid", "Ambiguous", "Unsupported"
    };

    public static readonly HashSet<string> AllowedOperations = new(StringComparer.Ordinal)
    {
        "morph.local",
        "morph.height",
        "material.recolor",
        "attachment.add",
        "attachment.remove",
        "creature.swapPart",
        "creature.replacePart",
        "creature.addPart",
        "garment.parameter.delta",
        "parameter.set",
        "parameter.delta",
        "material.pbr"
    };
}

public static class AiEditPlanValidator
{
    public static AiEditPlan Validate(AiEditPlan plan)
    {
        if (plan.SchemaVersion != AiEditPlanSchema.Version)
            return Reject("Unsupported schema.");
        if (!AiEditPlanSchema.AllowedStatuses.Contains(plan.Status))
            return Reject("Unknown plan status.");
        if (plan.Status != "valid")
            return plan;
        if (string.IsNullOrWhiteSpace(plan.Operation) || !AiEditPlanSchema.AllowedOperations.Contains(plan.Operation))
            return Reject("Operation is not in the allow-list.");
        if (!PromptSafety.ArgsAreSafe(plan.Args))
            return Reject("Plan args contain disallowed shell metacharacters.");
        var json = JsonSerializer.Serialize(plan);
        if (json.Contains("eval", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
            json.Contains("Process.Start", StringComparison.OrdinalIgnoreCase))
            return Reject("Plan contained disallowed content.");
        return plan;
    }

    private static AiEditPlan Reject(string reason) =>
        new() { Status = "Unsupported", Reason = reason, Provider = "validator" };
}
