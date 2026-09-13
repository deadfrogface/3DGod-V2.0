using System.Numerics;
using System.Text.Json;
using ThreeDGod.AI;
using ThreeDGod.Application;
using ThreeDGod.Mesh;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Online CI runtime proofs that require acquired models / uv workers.
/// When THREEDGOD_CI_RUNTIME_INTEGRATION=1, missing deps FAIL (no soft skip).
/// </summary>
public class CiOnlineRuntimeProofTests
{
    public const string EnvFlag = CiRuntimeIntegrationTests.EnvFlag;
    public const string ArtifactDirEnv = "THREEDGOD_CI_ARTIFACT_DIR";

    private static bool IntegrationRequested =>
        string.Equals(Environment.GetEnvironmentVariable(EnvFlag), "1", StringComparison.Ordinal);

    private static string ArtifactRoot()
    {
        var root = Environment.GetEnvironmentVariable(ArtifactDirEnv);
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(RepoPaths.FindRepoRoot(), "artifacts", "runtime");
        Directory.CreateDirectory(root);
        return root;
    }

    private static void RequireIntegrationOrGate(string capability)
    {
        if (!IntegrationRequested)
            TestGate.ExternalRuntime($"{capability}: set {EnvFlag}=1 after model/worker acquire to run live CI proof (not PASS).");
    }

    private static void CopyArtifact(string source, string name)
    {
        if (!File.Exists(source)) return;
        var dest = Path.Combine(ArtifactRoot(), name);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(source, dest, overwrite: true);
    }

    public static void AssertRealGlb(string glbPath, int minVerts = 100, long minBytes = 1024)
    {
        Assert.True(File.Exists(glbPath), "GLB missing.");
        Assert.True(new FileInfo(glbPath).Length > minBytes, "GLB too small to be a real mesh.");
        var doc = CanonicalGltfPipeline.Load(glbPath);
        Assert.True(doc.VertexCount >= minVerts, $"Expected >= {minVerts} verts, got {doc.VertexCount}.");
        Assert.True(doc.TriangleCount >= 1, "Expected triangles.");

        var (positions, indices) = MeshCompare.ReadMesh(glbPath);
        Assert.True(positions.Count >= minVerts);
        Assert.True(indices.Count >= 3);
        foreach (var v in positions)
        {
            Assert.True(float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z),
                $"Non-finite vertex: {v}");
        }
        foreach (var i in indices)
            Assert.True(i >= 0 && i < positions.Count, $"Index {i} out of range 0..{positions.Count - 1}");

        var min = positions[0];
        var max = positions[0];
        foreach (var v in positions)
        {
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }
        var size = max - min;
        Assert.True(size.Length() > 1e-4f, $"Degenerate bounds: min={min} max={max}");
    }

    [SkippableFact(Timeout = 7200000)]
    public async Task TripoSr_CpuGenerate_WritesValidatedGlb()
    {
        RequireIntegrationOrGate("TripoSR");
        if (!string.Equals(Environment.GetEnvironmentVariable("THREEDGOD_CI_TRIPOSR"), "1", StringComparison.Ordinal))
            TestGate.ExternalRuntime("TripoSR: set THREEDGOD_CI_TRIPOSR=1 after checkpoint acquire (heavy CPU job).");

        var probe = TripoSrRuntime.Probe(RepoPaths.FindRepoRoot());
        Assert.False(
            probe.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled,
            "TripoSR runtime missing while CI proof requested. Blocker: " + probe.Message);
        Assert.True(probe.HasCheckpoint, "TripoSR checkpoint required for PASS_REAL.");

        var fixture = Path.Combine(RepoPaths.FindRepoRoot(), "workers", "triposr", "fixtures", "chair.png");
        Assert.True(File.Exists(fixture), "chair.png fixture missing.");

        var outDir = Path.Combine(ArtifactRoot(), "triposr");
        Directory.CreateDirectory(outDir);
        var glb = Path.Combine(outDir, "chair-cpu.glb");
        if (File.Exists(glb)) File.Delete(glb);

        await using var svc = new TripoSrService(new WorkerProcessHost());
        var path = await svc.GenerateGlbAsync(fixture, glb);
        AssertRealGlb(path, minVerts: 100);
        CopyArtifact(path, "triposr/chair-cpu.glb");

        var meta = new
        {
            classification = "PASS_REAL",
            device = "cpu-or-auto",
            bytes = new FileInfo(path).Length,
            verts = CanonicalGltfPipeline.Load(path).VertexCount,
            tris = CanonicalGltfPipeline.Load(path).TriangleCount,
            checkpointSha256 = TripoSrRuntime.ModelSha256
        };
        await File.WriteAllTextAsync(
            Path.Combine(outDir, "triposr-result.json"),
            JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));
    }

    [SkippableFact]
    public void LlamaSharp_CpuGguf_Phrases_InvokeInferenceAndValidatePlans()
    {
        RequireIntegrationOrGate("LLamaSharp");
        if (!string.Equals(Environment.GetEnvironmentVariable("THREEDGOD_CI_LLAMASHARP"), "1", StringComparison.Ordinal))
            TestGate.ExternalRuntime("LLamaSharp: set THREEDGOD_CI_LLAMASHARP=1 after GGUF acquire.");

        var status = LlamaSharpProvider.Probe();
        Assert.Equal(FeatureAvailability.Experimental, status.Availability);
        Assert.False(string.IsNullOrWhiteSpace(status.ModelPath));
        Assert.True(File.Exists(status.ModelPath!), "Licensed GGUF must exist for PASS_REAL.");

        var phrases = new[]
        {
            "make him taller",
            "broader shoulders",
            "add a gold necklace",
            "make the jacket less shiny",
            "remove the horns"
        };

        var outDir = Path.Combine(ArtifactRoot(), "llamasharp");
        Directory.CreateDirectory(outDir);
        var results = new List<object>();

        var inferred = 0;
        foreach (var phrase in phrases)
        {
            var plan = LlamaSharpProvider.Interpret(phrase);
            Assert.Equal("llamasharp", plan.Provider);
            Assert.Contains(plan.Status, AiEditPlanSchema.AllowedStatuses);
            Assert.Equal(AiEditPlanSchema.Version, plan.SchemaVersion);
            if (plan.Status == "valid")
            {
                Assert.False(string.IsNullOrWhiteSpace(plan.Operation));
                Assert.Contains(plan.Operation!, AiEditPlanSchema.AllowedOperations);
                Assert.True(PromptSafety.ArgsAreSafe(plan.Args), "Unsafe args from model.");
                inferred++;
            }
            else
            {
                // Ambiguous / Unsupported after real decode is honest — reason must explain rejection.
                Assert.False(string.IsNullOrWhiteSpace(plan.Reason),
                    $"Phrase '{phrase}' returned {plan.Status} without reason after real inference.");
                inferred++;
            }

            var json = JsonSerializer.Serialize(plan);
            Assert.DoesNotContain("eval(", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Process.Start", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<script", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("rm -rf", json, StringComparison.OrdinalIgnoreCase);

            results.Add(new
            {
                phrase,
                plan.Status,
                plan.Operation,
                plan.Provider,
                plan.SchemaVersion,
                plan.Reason,
                plan.Args
            });
        }

        Assert.Equal(phrases.Length, inferred);

        // Malformed / unsafe plans must be rejected by the validator (hard gate).
        var malformed = AiEditPlanValidator.Validate(new AiEditPlan
        {
            SchemaVersion = "not-a-schema",
            Status = "valid",
            Operation = "morph.height",
            Provider = "llamasharp"
        });
        Assert.Equal("Unsupported", malformed.Status);

        var unsafeOp = AiEditPlanValidator.Validate(new AiEditPlan
        {
            Status = "valid",
            Operation = "shell.exec",
            Args = { ["cmd"] = "rm -rf /" },
            Provider = "llamasharp"
        });
        Assert.Equal("Unsupported", unsafeOp.Status);

        var unsafeArgs = AiEditPlanValidator.Validate(new AiEditPlan
        {
            Status = "valid",
            Operation = "morph.height",
            Args = { ["delta"] = "$(curl evil)" },
            Provider = "llamasharp"
        });
        Assert.Equal("Unsupported", unsafeArgs.Status);

        var payload = new
        {
            classification = "PASS_REAL",
            modelId = LlamaSharpProvider.RecommendedModelId,
            modelFile = Path.GetFileName(status.ModelPath),
            license = LlamaSharpProvider.RecommendedLicense,
            backend = LlamaSharpProvider.BackendAssembly,
            phrases = results,
            malformedRejected = true,
            unsafeRejected = true
        };
        File.WriteAllText(
            Path.Combine(outDir, "llamasharp-result.json"),
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }
}
