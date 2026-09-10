using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;

namespace ThreeDGod.AI;

public sealed class AiEditTarget
{
    public ParametricHumanState Human { get; init; } = new();
    public MaterialDefinition Material { get; init; } = new();
    public List<string> Attachments { get; init; } = [];
}

public static class AiEditExecutor
{
    public static async Task ExecuteAsync(IReadOnlyList<AiEditPlan> plans, AiEditTarget target, CommandStack stack, CancellationToken cancellationToken = default)
    {
        var valid = plans.Select(AiEditPlanValidator.Validate).Where(p => p.Status == "valid").ToArray();
        if (valid.Length == 0)
            throw new InvalidOperationException("Unsupported – no valid edit plan.");

        stack.BeginTransaction();
        try
        {
            foreach (var plan in valid)
                await stack.ExecuteAsync(ToCommand(plan, target), cancellationToken);
            await stack.CommitTransactionAsync("ai.composite", cancellationToken);
        }
        catch
        {
            await stack.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static IEditCommand ToCommand(AiEditPlan plan, AiEditTarget target)
    {
        return plan.Operation switch
        {
            "parameter.set" => SetParam(target.Human, plan, overwrite: true),
            "parameter.delta" => SetParam(target.Human, plan, overwrite: false),
            "morph.height" or "morph.local" => SetParam(target.Human, new AiEditPlan
            {
                Status = "valid",
                Operation = "parameter.delta",
                Args = new Dictionary<string, string>
                {
                    ["key"] = plan.Operation == "morph.height" ? "height" : plan.Args.GetValueOrDefault("region", "local"),
                    ["delta"] = plan.Args.GetValueOrDefault("delta", "0.1")
                }
            }, overwrite: false),
            "material.recolor" => Recolor(target.Material, plan),
            "material.pbr" => AdjustPbr(target.Material, plan),
            "attachment.add" => new CollectionChangeCommand<string>(target.Human.CreatedUtc.Ticks == 0 ? Guid.NewGuid() : Guid.NewGuid(), target.Attachments, plan.Args.GetValueOrDefault("type", "attachment"), add: true, "ai.attachment.add"),
            "attachment.remove" => new CollectionChangeCommand<string>(Guid.NewGuid(), target.Attachments, plan.Args.GetValueOrDefault("type", "attachment"), add: false, "ai.attachment.remove"),
            _ => throw new InvalidOperationException("Unsupported – operation has no executor.")
        };
    }

    private static PropertyChangeCommand SetParam(ParametricHumanState human, AiEditPlan plan, bool overwrite)
    {
        var key = plan.Args.GetValueOrDefault("key", "height");
        human.PhenotypeParameters.TryGetValue(key, out var old);
        var next = overwrite
            ? float.Parse(plan.Args.GetValueOrDefault("value", "0.5"), System.Globalization.CultureInfo.InvariantCulture)
            : old + float.Parse(plan.Args.GetValueOrDefault("delta", "0.1"), System.Globalization.CultureInfo.InvariantCulture);
        next = Math.Clamp(next, 0f, 1f);
        return new PropertyChangeCommand(Guid.NewGuid(), "param:" + key, old, next, value =>
        {
            human.PhenotypeParameters[key] = Convert.ToSingle(value);
        }, "ai.parameter");
    }

    private static PropertyChangeCommand Recolor(MaterialDefinition material, AiEditPlan plan)
    {
        var old = material.BaseColorFactor;
        var color = plan.Args.GetValueOrDefault("color", "red");
        var next = color switch
        {
            "darker" => new ColorRgba { R = Math.Max(0f, old.R * 0.6f), G = Math.Max(0f, old.G * 0.6f), B = Math.Max(0f, old.B * 0.6f), A = old.A },
            "red" => new ColorRgba { R = 0.8f, G = 0.1f, B = 0.1f, A = 1f },
            "gold" => new ColorRgba { R = 0.83f, G = 0.69f, B = 0.22f, A = 1f },
            _ => new ColorRgba { R = 0.4f, G = 0.4f, B = 0.4f, A = 1f }
        };
        return new PropertyChangeCommand(material.MaterialId, "material.baseColor", old, next, value =>
        {
            material.BaseColorFactor = (ColorRgba)value!;
        }, "ai.material");
    }

    private static PropertyChangeCommand AdjustPbr(MaterialDefinition material, AiEditPlan plan)
    {
        var oldMetal = material.MetallicFactor;
        var oldRough = material.RoughnessFactor;
        var oldColor = material.BaseColorFactor;
        var color = plan.Args.GetValueOrDefault("color", "");
        var nextColor = color switch
        {
            "gold" => new ColorRgba { R = 0.83f, G = 0.69f, B = 0.22f, A = 1f },
            "darker" => new ColorRgba { R = Math.Max(0f, oldColor.R * 0.6f), G = Math.Max(0f, oldColor.G * 0.6f), B = Math.Max(0f, oldColor.B * 0.6f), A = oldColor.A },
            _ => oldColor
        };
        var nextMetal = float.Parse(plan.Args.GetValueOrDefault("metallic", oldMetal.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.CultureInfo.InvariantCulture);
        var nextRough = float.Parse(plan.Args.GetValueOrDefault("roughness", oldRough.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.CultureInfo.InvariantCulture);
        nextMetal = Math.Clamp(nextMetal, 0f, 1f);
        nextRough = Math.Clamp(nextRough, 0f, 1f);
        // Encode triple change via composite-friendly sequential property writes on metallic as primary undo unit,
        // applying color+roughness in the same setter body.
        return new PropertyChangeCommand(material.MaterialId, "material.pbr", (oldMetal, oldRough, oldColor), (nextMetal, nextRough, nextColor), value =>
        {
            var (m, r, c) = ((float, float, ColorRgba))value!;
            material.MetallicFactor = m;
            material.RoughnessFactor = r;
            material.BaseColorFactor = c;
        }, "ai.material.pbr");
    }
}
