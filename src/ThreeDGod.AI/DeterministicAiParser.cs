using System.Text.Json;

namespace ThreeDGod.AI;

public sealed class AiEditPlan
{
    public string Status { get; init; } = "Unsupported";
    public string? Operation { get; init; }
    public Dictionary<string, string> Args { get; init; } = [];
}

public static class DeterministicAiParser
{
    public static AiEditPlan Parse(string prompt)
    {
        var p = prompt.Trim().ToLowerInvariant();
        return p switch
        {
            "shoulders wider" => Plan("valid", "morph.local", "region", "shoulders", "dir", "wider"),
            "jacket red" => Plan("valid", "material.recolor", "garment", "jacket", "color", "red"),
            "remove necklace" => Plan("valid", "attachment.remove", "type", "necklace"),
            "taller, keep head size" => Plan("valid", "morph.height", "preserve", "head"),
            "gold chain" => Plan("valid", "attachment.add", "type", "necklace", "material", "gold"),
            "rat head" => Plan("valid", "creature.swapPart", "slot", "head", "family", "rat"),
            _ => new AiEditPlan { Status = "Unsupported" }
        };
    }

    private static AiEditPlan Plan(string status, string op, params string[] kv)
    {
        var args = new Dictionary<string, string>();
        for (var i = 0; i + 1 < kv.Length; i += 2)
            args[kv[i]] = kv[i + 1];
        return new AiEditPlan { Status = status, Operation = op, Args = args };
    }

    public static string ToJson(AiEditPlan plan) => JsonSerializer.Serialize(plan);
}
