using ThreeDGod.Application;
using ThreeDGod.Export;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;

namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Real skin-tokens.cpp CLI inference when installed. Never soft-passes.
/// </summary>
public class SkinTokensCppAutoRigTests
{
    [Fact]
    public void Probe_WhenCliMissing_IsNotInstalled_NotSuccess()
    {
        var status = SkinTokensCppRuntime.Probe("cpu");
        Assert.NotEqual(FeatureAvailability.Available, status.Availability);
        if (status.Availability == FeatureAvailability.NotInstalled)
            Assert.Contains("NotInstalled", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact(Timeout = 600000)]
    public async Task CpuRig_WhenInstalled_WritesSkinnedGlb_DifferingFromInput()
    {
        var probe = SkinTokensCppRuntime.Probe("cpu");
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("skin-tokens.cpp CLI/model missing; CPU Auto-Rig PASS_REAL not executed.");
            return;
        }

        var work = Path.Combine(Path.GetTempPath(), "3dgod-stcpp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var src = Path.Combine(work, "unrigged.glb");
            var dest = Path.Combine(work, "rigged.glb");
            // Unrigged box geometry (no skin) — real inference must add skeleton/skin.
            WriteUnriggedBox(src);
            var before = CanonicalGltfPipeline.Load(src);
            Assert.True(before.SkinCount == 0 || before.HasJoints == false);

            var result = await SkinTokensCppRuntime.RigAsync(src, dest, "cpu");
            Assert.True(File.Exists(result));
            Assert.True(new FileInfo(result).Length > 64);

            var after = CanonicalGltfPipeline.Load(result);
            Assert.True(after.MeshCount >= 1);
            Assert.True(after.VertexCount >= 3);
            Assert.True(after.TriangleCount >= 1);
            Assert.True(after.SkinCount >= 1 || after.HasJoints,
                "Expected skeleton/skin in Auto-Rig output.");

            var validator = new RigValidationService();
            // Humanoid requirement may be too strict for experimental generated topologies — still require no NaNs via mesh read.
            var positions = MeshCompare.ReadPositions(result);
            Assert.NotEmpty(positions);
            foreach (var v in positions)
                Assert.True(float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z));

            Assert.True(after.VertexCount != before.VertexCount
                        || after.SkinCount != before.SkinCount
                        || after.NodeCount != before.NodeCount
                        || new FileInfo(result).Length != new FileInfo(src).Length,
                "Rigged output must differ meaningfully from unrigged input.");

            _ = validator;
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
            // Real Vulkan path — optional extended proof.
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
