using System.Text;
using System.Text.Json;
using LLama;
using LLama.Common;
using LLama.Sampling;
using ThreeDGod.Application;

namespace ThreeDGod.AI;

public sealed class LlamaRuntimeStatus
{
    public FeatureAvailability Availability { get; init; } = FeatureAvailability.NotInstalled;
    public string Message { get; init; } = "";
    public string? ModelPath { get; init; }
}

/// <summary>
/// Local GGUF free-form → AiEditPlan. Deterministic parser stays primary;
/// LLamaSharp only runs when a licensed Apache-2.0 (or clearer) GGUF is installed
/// and every model JSON must pass <see cref="AiEditPlanValidator"/>.
/// </summary>
public static class LlamaSharpProvider
{
    public const string RecommendedModelId = "Qwen/Qwen2.5-0.5B-Instruct-GGUF";
    public const string RecommendedFile = "qwen2.5-0.5b-instruct-q4_k_m.gguf";
    public const string RecommendedLicense = "apache-2.0";

    public static string BackendAssembly => typeof(LLamaWeights).Assembly.GetName().Name ?? "LLamaSharp";

    public static string GetDefaultModelDir()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Models", "llama");
        }

        var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var baseDir = string.IsNullOrWhiteSpace(xdg)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share")
            : xdg;
        return Path.Combine(baseDir, "3DGod", "Models", "llama");
    }

    public static LlamaRuntimeStatus Probe()
    {
        var dir = GetDefaultModelDir();
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
            var lic = doc.RootElement.TryGetProperty("licenseId", out var l) ? l.GetString() : null;
            if (lic is not null &&
                lic.Contains("llama", StringComparison.OrdinalIgnoreCase) &&
                lic.Contains("2", StringComparison.OrdinalIgnoreCase))
            {
                // Meta Llama 2 community terms are not treated as freely redistributable product defaults.
                return new LlamaRuntimeStatus { Availability = FeatureAvailability.Disabled, Message = "LicenseBlocked – Llama-2-style community license is not an accepted product default." };
            }
        }
        catch (Exception ex)
        {
            return new LlamaRuntimeStatus { Availability = FeatureAvailability.Disabled, Message = "LicenseBlocked – " + ex.Message };
        }

        var gguf = Directory.GetFiles(dir, "*.gguf").OrderBy(f => new FileInfo(f).Length).FirstOrDefault();
        if (gguf is null)
            return new LlamaRuntimeStatus { Availability = FeatureAvailability.NotInstalled, Message = "NotInstalled – license accepted but no GGUF is installed." };

        return new LlamaRuntimeStatus
        {
            Availability = FeatureAvailability.Experimental,
            Message = $"Experimental – licensed GGUF present ({Path.GetFileName(gguf)}). Free-form output must validate as AiEditPlan.",
            ModelPath = gguf
        };
    }

    public static AiEditPlan Interpret(string prompt)
    {
        var status = Probe();
        if (status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.Disabled or FeatureAvailability.UnsupportedHardware)
            return new AiEditPlan { Status = "Unsupported", Provider = "llamasharp", Reason = status.Message };
        if (string.IsNullOrWhiteSpace(status.ModelPath) || !File.Exists(status.ModelPath))
            return new AiEditPlan { Status = "Unsupported", Provider = "llamasharp", Reason = "NotInstalled – GGUF path missing." };

        try
        {
            var raw = RunGguf(status.ModelPath, prompt);
            var plan = TryParsePlan(raw);
            if (plan is null)
            {
                return new AiEditPlan
                {
                    Status = "Unsupported",
                    Provider = "llamasharp",
                    Reason = "Malformed model output rejected (no valid AiEditPlan JSON)."
                };
            }

            plan = new AiEditPlan
            {
                SchemaVersion = plan.SchemaVersion,
                Status = plan.Status,
                Operation = plan.Operation,
                Args = plan.Args,
                Provider = "llamasharp",
                Reason = plan.Reason
            };
            return AiEditPlanValidator.Validate(plan);
        }
        catch (Exception ex)
        {
            return new AiEditPlan
            {
                Status = "Unsupported",
                Provider = "llamasharp",
                Reason = "LLamaSharp inference failed: " + ex.Message
            };
        }
    }

    private static string RunGguf(string modelPath, string userPrompt)
    {
        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 2048,
            GpuLayerCount = 0
        };
        using var weights = LLamaWeights.LoadFromFile(parameters);
        var executor = new StatelessExecutor(weights, parameters);

        var system = """
You convert a short 3D God edit instruction into ONE JSON object only.
Schema:
{"schemaVersion":"3dgod-ai-edit/1","status":"valid","operation":"<op>","args":{"k":"v"},"provider":"llamasharp"}
Allowed operations: morph.local, morph.height, material.recolor, attachment.add, attachment.remove, creature.swapPart, creature.replacePart, creature.addPart, garment.parameter.delta, parameter.set, parameter.delta, material.pbr
If unclear, return {"schemaVersion":"3dgod-ai-edit/1","status":"Ambiguous","provider":"llamasharp","reason":"..."}.
Never include shell, URLs, code, filesystem paths, or extra keys. JSON only.
""";

        var prompt = $"<|im_start|>system\n{system}<|im_end|>\n<|im_start|>user\n{userPrompt}<|im_end|>\n<|im_start|>assistant\n";
        var sb = new StringBuilder();
        var infer = executor.InferAsync(prompt, new InferenceParams
        {
            MaxTokens = 256,
            AntiPrompts = ["<|im_end|>", "```"],
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.1f }
        });
        foreach (var token in infer.ToBlockingEnumerable())
        {
            sb.Append(token);
            if (sb.Length > 2000)
                break;
        }

        return sb.ToString().Trim();
    }

    private static AiEditPlan? TryParsePlan(string raw)
    {
        var json = ExtractJsonObject(raw);
        if (json is null)
            return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "Unsupported" : "Unsupported";
            var op = root.TryGetProperty("operation", out var o) ? o.GetString() : null;
            var reason = root.TryGetProperty("reason", out var r) ? r.GetString() : null;
            var schema = root.TryGetProperty("schemaVersion", out var sv) ? sv.GetString() : AiEditPlanSchema.Version;
            var args = new Dictionary<string, string>(StringComparer.Ordinal);
            if (root.TryGetProperty("args", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in argsEl.EnumerateObject())
                    args[p.Name] = p.Value.ValueKind == JsonValueKind.String ? (p.Value.GetString() ?? "") : p.Value.ToString();
            }

            return new AiEditPlan
            {
                SchemaVersion = schema ?? AiEditPlanSchema.Version,
                Status = status,
                Operation = op,
                Args = args,
                Provider = "llamasharp",
                Reason = reason
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;
        return raw[start..(end + 1)];
    }

    /// <summary>Writes an accepted license sidecar for an Apache-2.0 (or similarly clear) GGUF install.</summary>
    public static void WriteAcceptedLicense(string modelDir, string licenseId, string modelId, string sourceUrl)
    {
        Directory.CreateDirectory(modelDir);
        var path = Path.Combine(modelDir, "license.json");
        var json = JsonSerializer.Serialize(new
        {
            accepted = true,
            licenseId,
            modelId,
            sourceUrl,
            acceptedUtc = DateTime.UtcNow.ToString("O")
        });
        File.WriteAllText(path, json);
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
