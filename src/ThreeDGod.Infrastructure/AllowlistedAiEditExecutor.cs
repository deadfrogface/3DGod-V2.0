using ThreeDGod.AI;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;

namespace ThreeDGod.Infrastructure;

/// <summary>
/// Executes allowlisted deterministic AI edit plans against the active project.
/// Does not invent LLM codegen; refuses unsupported/ambiguous plans.
/// </summary>
public sealed class AllowlistedAiEditExecutor
{
    private readonly ActiveProjectSession _session;

    public AllowlistedAiEditExecutor(ActiveProjectSession session)
    {
        _session = session;
    }

    public Task<AiEditExecutionResult> ExecutePromptAsync(
        string prompt,
        CommandStack? stack = null,
        CancellationToken cancellationToken = default)
    {
        var plan = DeterministicAiParser.Parse(prompt);
        return ExecutePlanAsync(plan, stack, cancellationToken);
    }

    public Task<AiEditExecutionResult> ExecutePlanAsync(
        AiEditPlan plan,
        CommandStack? stack = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(plan.Status, "valid", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new AiEditExecutionResult(
                false,
                plan.Status,
                plan.Operation ?? "",
                plan.Reason ?? $"Plan status '{plan.Status}' is not executable."));
        }

        return plan.Operation switch
        {
            "morph.height" => ExecuteHeightDelta(plan, stack, 0.1f),
            "parameter.delta" when IsHeightKey(plan) => ExecuteHeightDelta(plan, stack, ReadDelta(plan, 0.15f)),
            "material.pbr" or "material.recolor" => ExecuteMaterial(plan),
            "attachment.remove" or "attachment.add" => Task.FromResult(new AiEditExecutionResult(
                false,
                "Gated",
                plan.Operation ?? "",
                "Attachment mesh pipeline is NotImplemented – plan parsed but not faked.")),
            "creature.replacePart" or "creature.addPart" or "garment.parameter.delta" => Task.FromResult(new AiEditExecutionResult(
                false,
                "BackendOnly",
                plan.Operation ?? "",
                "Creature/garment catalog edits require creature workflow UI; use dedicated services.")),
            "morph.local" => ExecuteLocalMorph(plan),
            _ => Task.FromResult(new AiEditExecutionResult(
                false,
                "Unsupported",
                plan.Operation ?? "",
                $"Operation '{plan.Operation}' is not allowlisted for product execution."))
        };
    }

    private static bool IsHeightKey(AiEditPlan plan) =>
        !plan.Args.TryGetValue("key", out var key) ||
        string.Equals(key, "height", StringComparison.OrdinalIgnoreCase);

    private static float ReadDelta(AiEditPlan plan, float fallback)
    {
        if (plan.Args.TryGetValue("delta", out var deltaText) &&
            float.TryParse(deltaText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return fallback;
    }

    private Task<AiEditExecutionResult> ExecuteHeightDelta(AiEditPlan plan, CommandStack? stack, float delta)
    {
        var character = _session.ActiveCharacter
            ?? throw new InvalidOperationException("No active character.");
        var state = character.ParametricHumanState
            ?? new ParametricHumanState { BackendId = "anny", TopologyProfile = "anny", RigProfile = "anny" };

        const string key = "height";
        var old = state.PhenotypeParameters.GetValueOrDefault(key, 0.5f);
        var next = Math.Clamp(old + delta, 0f, 1f);
        var newState = DomainJson.Deserialize<ParametricHumanState>(DomainJson.Serialize(state));
        newState.PhenotypeParameters[key] = next;

        if (stack is not null)
        {
            _ = stack.ExecuteAsync(new PropertyChangeCommand(
                character.CharacterId,
                "anny.height",
                old,
                next,
                value =>
                {
                    var s = DomainJson.Deserialize<ParametricHumanState>(DomainJson.Serialize(newState));
                    s.PhenotypeParameters[key] = Convert.ToSingle(value);
                    _session.SetAnnyState(s);
                },
                "ai.morph.height"));
        }

        _session.SetAnnyState(newState);
        return Task.FromResult(new AiEditExecutionResult(
            true,
            "Executed",
            plan.Operation ?? "morph.height",
            $"height {old:0.###} → {next:0.###} (Anny phenotype, not uniform scale)."));
    }

    private Task<AiEditExecutionResult> ExecuteLocalMorph(AiEditPlan plan)
    {
        var character = _session.ActiveCharacter
            ?? throw new InvalidOperationException("No active character.");
        var state = character.ParametricHumanState
            ?? new ParametricHumanState { BackendId = "anny", TopologyProfile = "anny", RigProfile = "anny" };
        var region = plan.Args.GetValueOrDefault("region") ?? "shoulders";
        var old = state.LocalShapeParameters.GetValueOrDefault(region, 0f);
        var next = Math.Clamp(old + 0.15f, -1f, 1f);
        var newState = DomainJson.Deserialize<ParametricHumanState>(DomainJson.Serialize(state));
        newState.LocalShapeParameters[region] = next;
        _session.SetAnnyState(newState);
        return Task.FromResult(new AiEditExecutionResult(
            true,
            "Executed",
            plan.Operation ?? "morph.local",
            $"local '{region}' {old:0.###} → {next:0.###}."));
    }

    private Task<AiEditExecutionResult> ExecuteMaterial(AiEditPlan plan)
    {
        var metallic = 0.2f;
        var roughness = 0.6f;
        if (plan.Args.TryGetValue("metallic", out var m) &&
            float.TryParse(m, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var mv))
            metallic = mv;
        if (plan.Args.TryGetValue("roughness", out var r) &&
            float.TryParse(r, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rv))
            roughness = rv;

        float cr = 0.8f, cg = 0.6f, cb = 0.5f;
        var color = plan.Args.GetValueOrDefault("color") ?? "";
        if (color.Contains("gold", StringComparison.OrdinalIgnoreCase))
        {
            cr = 0.83f;
            cg = 0.69f;
            cb = 0.22f;
            metallic = plan.Args.ContainsKey("metallic") ? metallic : 0.85f;
            roughness = plan.Args.ContainsKey("roughness") ? roughness : 0.35f;
        }
        else if (color.Contains("red", StringComparison.OrdinalIgnoreCase))
        {
            cr = 0.75f;
            cg = 0.12f;
            cb = 0.12f;
        }
        else if (color.Contains("dark", StringComparison.OrdinalIgnoreCase))
        {
            cr = 0.25f;
            cg = 0.18f;
            cb = 0.14f;
        }

        _session.UpsertMaterial("skin", cr, cg, cb, 1f, metallic, roughness);
        return Task.FromResult(new AiEditExecutionResult(
            true,
            "Executed",
            plan.Operation ?? "material",
            $"material skin updated (metallic={metallic:0.##}, roughness={roughness:0.##})."));
    }
}

public sealed record AiEditExecutionResult(bool Ok, string Status, string Operation, string Message);
