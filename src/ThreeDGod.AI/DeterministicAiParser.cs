using System.Text.Json;

namespace ThreeDGod.AI;

public static class DeterministicAiParser
{
    public static AiEditPlan Parse(string prompt)
    {
        var p = prompt.Trim().ToLowerInvariant();
        var plan = p switch
        {
            "shoulders wider" => Plan("valid", "morph.local", "deterministic", "region", "shoulders", "dir", "wider"),
            "jacket red" => Plan("valid", "material.recolor", "deterministic", "garment", "jacket", "color", "red"),
            "remove necklace" => Plan("valid", "attachment.remove", "deterministic", "type", "necklace"),
            "taller, keep head size" => Plan("valid", "morph.height", "deterministic", "preserve", "head"),
            "gold chain" => Plan("valid", "attachment.add", "deterministic", "type", "necklace", "material", "gold"),
            "rat head" => Plan("valid", "creature.swapPart", "deterministic", "slot", "head", "family", "rat"),
            "make it nicer" or "improve" => new AiEditPlan { Status = "Ambiguous", Provider = "deterministic", Reason = "Prompt is too vague." },
            _ => new AiEditPlan { Status = "Unsupported", Provider = "deterministic" }
        };
        return AiEditPlanValidator.Validate(plan);
    }

    private static AiEditPlan Plan(string status, string op, string provider, params string[] kv)
    {
        var args = new Dictionary<string, string>();
        for (var i = 0; i + 1 < kv.Length; i += 2)
            args[kv[i]] = kv[i + 1];
        return new AiEditPlan { Status = status, Operation = op, Args = args, Provider = provider };
    }

    public static string ToJson(AiEditPlan plan) => JsonSerializer.Serialize(plan);
}
