using System.Text.Json;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

[CollectionDefinition("GarmentCodeSerial", DisableParallelization = true)]
public class GarmentCodeSerialCollection;

[Collection("GarmentCodeSerial")]
public class GarmentCodeTests
{
    [Fact]
    public void Probe_NeverReportsSuccess()
    {
        var status = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        Assert.False(string.IsNullOrWhiteSpace(status.Message));
        Assert.DoesNotContain("success", status.Message, StringComparison.OrdinalIgnoreCase);
        if (status.Availability == FeatureAvailability.NotInstalled)
            Assert.Contains("NotInstalled", status.Message, StringComparison.Ordinal);
        Assert.Equal(status.Availability, new DynamicFeatureAvailabilityService().GetStatus(FeatureIds.GarmentCode));
    }

    [Fact]
    public void Probe_IsExperimental_WhenUvLockExists()
    {
        var probe = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        var lockFile = Path.Combine(RepoPaths.FindRepoRoot(), "workers", "garmentcode", "uv.lock");
        if (!File.Exists(lockFile) || AnnyRuntime.FindUv() is null)
        {
            Assert.Equal(FeatureAvailability.NotInstalled, probe.Availability);
            return;
        }
        Assert.Equal(FeatureAvailability.Experimental, probe.Availability);
        Assert.Contains("pygarment", probe.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not bundled", probe.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("success", probe.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Generate_WhenNotInstalled_ThrowsAndWritesNothing()
    {
        var probe = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is FeatureAvailability.Available or FeatureAvailability.Experimental)
        {
            TestGate.ExternalDependency("GarmentCode runtime installed; NotInstalled throw path not exercised.");
            return;
        }
        await using var svc = new GarmentCodeService(new WorkerProcessHost());
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".glb");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GenerateJacketGlbAsync(dest));
        Assert.Contains("NotInstalled", ex.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(dest));
        Assert.False(File.Exists(Path.ChangeExtension(dest, ".obj")));
    }

    [SkippableFact]
    public async Task Generate_WhenInstalled_WritesPatternWithFourPanelsAndGlb()
    {
        var probe = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("GarmentCode runtime missing; jacket generate test not executed.");
            return;
        }

        var dest = Path.Combine(Path.GetTempPath(), "3dgod-gc-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            await using var svc = new GarmentCodeService(new WorkerProcessHost());
            var glb = await svc.GenerateJacketGlbAsync(dest, new GarmentCodeJacketRequest { SleeveLengthCm = 45, LengthCm = 60, WidthCm = 40 });
            Assert.True(File.Exists(glb));
            var obj = Path.ChangeExtension(dest, ".obj");
            var pattern = Path.ChangeExtension(dest, ".json");
            Assert.True(File.Exists(obj), "Expected pygarment OBJ on disk.");
            Assert.True(File.Exists(pattern), "Expected pygarment pattern JSON on disk.");
            using var doc = JsonDocument.Parse(File.ReadAllText(pattern));
            Assert.True(doc.RootElement.TryGetProperty("panels", out var panels));
            Assert.True(panels.EnumerateObject().Count() >= 4, "Expected >=4 sewing panels.");
            Assert.True(panels.TryGetProperty("jacket-front", out _));
            Assert.True(panels.TryGetProperty("jacket-sleeve-L", out _));
            var loaded = CanonicalGltfPipeline.Load(glb);
            Assert.True(loaded.VertexCount >= 12, $"Expected a real jacket mesh, got {loaded.VertexCount} vertices.");
            Assert.True(loaded.TriangleCount >= 4, $"Expected triangulated panels, got {loaded.TriangleCount} triangles.");
        }
        finally
        {
            foreach (var path in new[] { dest, Path.ChangeExtension(dest, ".obj"), Path.ChangeExtension(dest, ".json") })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    [SkippableFact]
    public async Task LongerSleeves_ChangePatternGeometry()
    {
        var probe = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("GarmentCode runtime missing; sleeve geometry compare not executed.");
            return;
        }

        var shortPath = Path.Combine(Path.GetTempPath(), "3dgod-gc-short-" + Guid.NewGuid().ToString("N") + ".glb");
        var longPath = Path.Combine(Path.GetTempPath(), "3dgod-gc-long-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            await using var svc = new GarmentCodeService(new WorkerProcessHost());
            await svc.GenerateJacketGlbAsync(shortPath, new GarmentCodeJacketRequest { SleeveLengthCm = 30, LengthCm = 60, WidthCm = 40 });
            await svc.GenerateJacketGlbAsync(longPath, new GarmentCodeJacketRequest { SleeveLengthCm = 70, LengthCm = 60, WidthCm = 40 });
            var shortPos = MeshCompare.ReadPositions(shortPath);
            var longPos = MeshCompare.ReadPositions(longPath);
            var shortSpan = shortPos.Max(p => MathF.Abs(p.X));
            var longSpan = longPos.Max(p => MathF.Abs(p.X));
            Assert.True(longSpan > shortSpan + 0.15f, $"Sleeve span did not grow: {shortSpan} -> {longSpan}");
        }
        finally
        {
            foreach (var path in new[] { shortPath, longPath, Path.ChangeExtension(shortPath, ".obj"), Path.ChangeExtension(longPath, ".obj"), Path.ChangeExtension(shortPath, ".json"), Path.ChangeExtension(longPath, ".json") })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
