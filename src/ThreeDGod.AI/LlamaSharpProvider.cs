using System.Text.Json;
using ThreeDGod.Application;

namespace ThreeDGod.AI;

public sealed class LlamaRuntimeStatus
{
    public FeatureAvailability Availability { get; init; } = FeatureAvailability.NotInstalled;
    public string Message { get; init; } = "";
    public string? ModelPath { get; init; }
}

public static class LlamaSharpProvider
{
    public static string BackendAssembly => typeof(LLama.LLamaWeights).Assembly.GetName().Name ?? "LLamaSharp";

    public static LlamaRuntimeStatus Probe()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod", "Models", "llama");
        var license = Path.Combine(dir, "license.json");
        if (!Directory.Exists(dir))
            return new LlamaRuntimeStatus { Availability = FeatureAvailability.NotInstalled, Message = "NotInstalled – no LLamaSharp GGUF directory." };
        if (!File.Exists(license))
            return new LlamaRuntimeStatus { Availability = FeatureAvailability.Disabled, Message = "LicenseBlocked – GGUF folder has no accepted license.json." };
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(license));
            var accepted = doc.RootElement.TryGetProperty("accepted", out var a) && a.ValueKind == JsonValueKind.True;
            if (!accepted)
                return new LlamaRuntimeStatus { Availability = FeatureAvailability.Disabled, Message = "LicenseBlocked – license.json is not accepted." };
        }
        catch (Exception ex)
        {
            return new LlamaRuntimeStatus { Availability = FeatureAvailability.Disabled, Message = "LicenseBlocked – " + ex.Message };
        }

        var gguf = Directory.GetFiles(dir, "*.gguf").FirstOrDefault();
        if (gguf is null)
            return new LlamaRuntimeStatus { Availability = FeatureAvailability.NotInstalled, Message = "NotInstalled – license accepted but no GGUF is installed." };

        return new LlamaRuntimeStatus
        {
            Availability = FeatureAvailability.Experimental,
            Message = "Experimental – licensed GGUF present. LLamaSharp inference is wired but not claimed as production-ready.",
            ModelPath = gguf
        };
    }

    public static AiEditPlan Interpret(string prompt)
    {
        var status = Probe();
        if (status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.Disabled or FeatureAvailability.UnsupportedHardware)
            return new AiEditPlan { Status = "Unsupported", Provider = "llamasharp", Reason = status.Message };
        // A licensed GGUF exists. We still do not execute arbitrary model JSON.
        // The model output must pass the same validator; without a verified prompt-to-plan
        // contract we refuse to invent operations.
        return AiEditPlanValidator.Validate(new AiEditPlan
        {
            Status = "Unsupported",
            Provider = "llamasharp",
            Reason = "Experimental GGUF is present but no verified prompt-to-plan mapping is implemented. Deterministic parser remains the only valid producer."
        });
    }
}

public sealed class AiCommandInterpreter
{
    public AiEditPlan Interpret(string prompt)
    {
        var deterministic = DeterministicAiParser.Parse(prompt);
        if (deterministic.Status is "valid" or "Ambiguous")
            return deterministic;
        return LlamaSharpProvider.Interpret(prompt);
    }
}
