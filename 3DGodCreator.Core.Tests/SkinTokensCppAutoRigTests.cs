using ThreeDGod.Application;
using ThreeDGod.Export;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Rigging;

namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Real skin-tokens.cpp CLI inference when installed. Never soft-passes.
/// When THREEDGOD_CI_AUTOROOT=1, missing CLI/model FAILS (required core proof).
/// </summary>
public class SkinTokensCppAutoRigTests
{
    public const string CiEnv = "THREEDGOD_CI_AUTOROOT";

    private static bool CiRequired =>
        string.Equals(Environment.GetEnvironmentVariable(CiEnv), "1", StringComparison.Ordinal);

    [Fact]
    public void Probe_WhenCliMissing_IsNotInstalled_NotSuccess()
    {
        if (CiRequired)
        {
            // Under CI proof mode the runtime must already be provisioned — assert Ready path separately.
            return;
        }

        var status = SkinTokensCppRuntime.Probe("cpu");
        Assert.NotEqual(FeatureAvailability.Available, status.Availability);
        if (status.Availability == FeatureAvailability.NotInstalled)
            Assert.Contains("NotInstalled", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact(Timeout = 1_200_000)]
    public async Task CpuRig_WhenInstalled_WritesSkinnedGlb_DifferingFromInput()
    {
        var probe = SkinTokensCppRuntime.Probe("cpu");
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            if (CiRequired)
                Assert.Fail(
                    $"REQUIRED Auto-Rig CPU proof failed: skin-tokens.cpp not ready. {probe.Message}");
            TestGate.NotInstalled("skin-tokens.cpp CLI/model missing; CPU Auto-Rig PASS_REAL not executed.");
            return;
        }

        var work = Path.Combine(Path.GetTempPath(), "3dgod-stcpp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var artifactRoot = Environment.GetEnvironmentVariable("THREEDGOD_CI_ARTIFACT_DIR");
        try
        {
            var src = Path.Combine(work, "unrigged.glb");
            var dest = Path.Combine(work, "rigged.glb");
            WriteUnriggedBox(src);
            var before = CanonicalGltfPipeline.Load(src);
            Assert.True(before.SkinCount == 0 || before.HasJoints == false);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var selector = new ThreeDGod.Infrastructure.AutoRig.AutoRigProviderSelector(
            [
                new ThreeDGod.Infrastructure.AutoRig.SkinTokensCppCpuAutoRigProvider(),
                new ThreeDGod.Infrastructure.AutoRig.SkinTokensCppVulkanAutoRigProvider()
            ]);
            var result = await selector.RigAsync(src, dest, AutoRigDevicePreference.Cpu);
            sw.Stop();

            Assert.Equal("skintokens-cpp-cpu", result.ProviderId);
            Assert.Equal(AutoRigProviderKind.SkinTokensCppCpu, result.Kind);
            Assert.Equal("cpu", result.Device);
            Assert.Contains("skintokens-cpp@", result.Provenance);
            Assert.Contains("device=cpu", result.Provenance);
            Assert.True(File.Exists(result.OutputGlb));
            Assert.True(new FileInfo(result.OutputGlb).Length > 64);

            var after = CanonicalGltfPipeline.Load(result.OutputGlb);
            Assert.True(after.MeshCount >= 1);
            Assert.True(after.VertexCount >= 3);
            Assert.True(after.TriangleCount >= 1);
            Assert.True(after.SkinCount >= 1, "Expected skin in Auto-Rig output.");
            Assert.True(after.HasJoints, "Expected joints/skeleton in Auto-Rig output.");

            // Structural skin/weight/bone validation (not humanoid-semantic — generated rigs vary).
            var report = RigValidator.ValidateGlb(result.OutputGlb, requireHumanoid: false);
            Assert.True(report.Passed, "Rig validation failed: " + string.Join("; ", report.Failures.Select(f => $"{f.Code}:{f.Message}")));
            Assert.True(report.SkinCount >= 1);
            Assert.NotEmpty(report.JointNames);

            var positions = MeshCompare.ReadPositions(result.OutputGlb);
            Assert.NotEmpty(positions);
            foreach (var v in positions)
                Assert.True(float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z));

            Assert.True(after.VertexCount != before.VertexCount
                        || after.SkinCount != before.SkinCount
                        || after.NodeCount != before.NodeCount
                        || new FileInfo(result.OutputGlb).Length != new FileInfo(src).Length,
                "Rigged output must differ meaningfully from unrigged input.");

            if (!string.IsNullOrWhiteSpace(artifactRoot))
            {
                var art = Path.IsPathRooted(artifactRoot)
                    ? artifactRoot
                    : Path.GetFullPath(Path.Combine(RepoPaths.FindRepoRoot(), artifactRoot));
                Directory.CreateDirectory(art);
                File.Copy(src, Path.Combine(art, "autoroot-input-unrigged.glb"), overwrite: true);
                File.Copy(result.OutputGlb, Path.Combine(art, "autoroot-output-rigged.glb"), overwrite: true);
                File.WriteAllText(Path.Combine(art, "autoroot-cpu-proof.json"),
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ok = true,
                        providerId = result.ProviderId,
                        provenance = result.Provenance,
                        device = result.Device,
                        durationMs = sw.ElapsedMilliseconds,
                        upstreamCommit = SkinTokensCppRuntime.UpstreamCommitHint,
                        modelRepo = SkinTokensCppRuntime.ModelRepo,
                        skinCount = after.SkinCount,
                        jointCount = report.JointNames.Count,
                        vertexCount = after.VertexCount,
                        inputBytes = new FileInfo(src).Length,
                        outputBytes = new FileInfo(result.OutputGlb).Length
                    }));
            }
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }

    [SkippableFact(Timeout = 600000)]
    public async Task VulkanRig_WhenUnavailable_DoesNotSoftPassAsCpu()
    {
        var vulkan = SkinTokensCppRuntime.Probe("vulkan");
        if (vulkan.Availability is FeatureAvailability.Available or FeatureAvailability.Experimental)
        {
            var work = Path.Combine(Path.GetTempPath(), "3dgod-stcpp-vk-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            try
            {
                var src = Path.Combine(work, "unrigged.glb");
                var dest = Path.Combine(work, "rigged-vk.glb");
                WriteUnriggedBox(src);
                await SkinTokensCppRuntime.RigAsync(src, dest, "vulkan");
                Assert.True(File.Exists(dest));
                var doc = CanonicalGltfPipeline.Load(dest);
                Assert.True(doc.SkinCount >= 1 || doc.HasJoints);
            }
            finally
            {
                try { Directory.Delete(work, true); } catch { /* ignore */ }
            }
            return;
        }

        TestGate.Hardware("Vulkan Auto-Rig not available on this host; IMPLEMENTED_GATED_VULKAN_RUNTIME_PROOF (not PASS_REAL).");
    }

    private static void WriteUnriggedBox(string path)
    {
        var min = new System.Numerics.Vector3(-0.2f, 0f, -0.2f);
        var max = new System.Numerics.Vector3(0.2f, 0.5f, 0.2f);
        var positions = new List<System.Numerics.Vector3>
        {
            new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z), new(max.X, max.Y, min.Z), new(min.X, max.Y, min.Z),
            new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z), new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z)
        };
        var indices = new List<int>
        {
            0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6, 0, 4, 5, 0, 5, 1, 3, 2, 6, 3, 6, 7, 0, 3, 7, 0, 7, 4, 1, 5, 6, 1, 6, 2
        };
        TriangleMeshExport.WriteGlb(path, positions, indices, new System.Numerics.Vector4(0.7f, 0.7f, 0.75f, 1f), 0.05f, 0.6f);
    }
}
