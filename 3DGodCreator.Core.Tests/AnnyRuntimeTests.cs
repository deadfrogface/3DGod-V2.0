using System.Numerics;
using ThreeDGod.Application;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

[Collection("AnnySerial")]
public class AnnyRuntimeTests
{
    [Fact]
    public void TriangleMeshExport_WritesLoadableGlb()
    {
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            TriangleMeshExport.WriteGlb(
                dest,
                [new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)],
                [0, 1, 2]);
            var doc = CanonicalGltfPipeline.Load(dest);
            Assert.True(doc.VertexCount >= 3);
            Assert.True(doc.TriangleCount >= 1);
        }
        finally
        {
            if (File.Exists(dest)) File.Delete(dest);
        }
    }

    [Fact]
    public void AnnyProbe_NeverReportsSuccessWithoutRuntime()
    {
        var status = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        Assert.False(string.IsNullOrWhiteSpace(status.Message));
        Assert.DoesNotContain("success", status.Message, StringComparison.OrdinalIgnoreCase);
        if (status.Availability == FeatureAvailability.NotInstalled)
            Assert.Contains("NotInstalled", status.Message, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task AnnyGenerate_WhenNotInstalled_ThrowsHonestError()
    {
        var probe = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is FeatureAvailability.Available or FeatureAvailability.Experimental)
        {
            TestGate.ExternalDependency("Anny runtime installed; NotInstalled throw path not exercised.");
            return;
        }
        var svc = new AnnyHumanService(new WorkerProcessHost());
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".glb");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GenerateGlbAsync(dest));
        Assert.Contains("NotInstalled", ex.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(dest));
    }

    [Fact]
    public void AnnyProbe_ExperimentalIsNotAvailable()
    {
        var probe = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        Assert.NotEqual(FeatureAvailability.Available, probe.Availability);
        Assert.DoesNotContain("success", probe.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public void AnnyPresetData_DoesNotImplyRuntimeExecution()
    {
        var store = new AnnyPresetStore(RepoPaths.FindRepoRoot());
        var presets = store.List();
        Assert.NotEmpty(presets);
        var probe = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is FeatureAvailability.NotInstalled)
            TestGate.NotInstalled("Preset JSON exists; live Anny mesh apply remains gated.");
    }

    [SkippableFact]
    public async Task AnnyGenerate_WhenInstalled_WritesRealGlb()
    {
        var probe = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("Anny uv/runtime missing; live generate not executed.");
            return;
        }
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-anny-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            var svc = new AnnyHumanService(new WorkerProcessHost());
            var glb = await svc.GenerateGlbAsync(dest);
            Assert.True(File.Exists(glb));
            var doc = CanonicalGltfPipeline.Load(glb);
            Assert.True(doc.VertexCount > 1000, $"Expected a real Anny mesh, got {doc.VertexCount} vertices.");
            Assert.True(doc.TriangleCount > 1000, $"Expected a real Anny mesh, got {doc.TriangleCount} triangles.");
        }
        finally
        {
            if (File.Exists(dest)) File.Delete(dest);
            var obj = Path.ChangeExtension(dest, ".obj");
            if (File.Exists(obj)) File.Delete(obj);
        }
    }
}
