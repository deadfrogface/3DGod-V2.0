using System.Text.Json;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

public class GarmentFitTests
{
    [Fact]
    public void Measure_MaleBase_HasHumanScaleHeight()
    {
        var src = Path.Combine(RepoPaths.AssetsDir, "characters", "male_base.glb");
        var pos = MeshCompare.ReadPositions(src);
        var m = GarmentProximityFitter.Measure(pos, src);
        Assert.True(m.HeightM > 0.8f, $"Expected a standing human height, got {m.HeightM}m.");
        Assert.True(m.ChestWidthM > 0.15f);
        Assert.True(m.TorsoCenterY > m.MinY);
    }

    [SkippableFact]
    public async Task FitJacket_OnMaleBase_WritesClippingReport()
    {
        var probe = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("GarmentCode runtime missing; jacket fit on male_base not executed.");
            return;
        }

        var body = Path.Combine(RepoPaths.AssetsDir, "characters", "male_base.glb");
        var work = Path.Combine(Path.GetTempPath(), "3dgod-fit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            await using var garment = new GarmentCodeService(new WorkerProcessHost());
            var svc = new GarmentFitService(new AnnyHumanService(new WorkerProcessHost()), garment);
            var result = await svc.FitJacketToBodyAsync(body, work, "male_base");
            Assert.True(File.Exists(result.FittedGlb));
            Assert.True(File.Exists(result.ReportPath));
            Assert.Equal(0, result.Report.InsideAfter);
            Assert.True(result.Report.SampleCount >= 12);
            using var doc = JsonDocument.Parse(File.ReadAllText(result.ReportPath));
            Assert.Equal(0, doc.RootElement.GetProperty("InsideAfter").GetInt32());
            Assert.Contains("geometry3Sharp", doc.RootElement.GetProperty("Backend").GetString(), StringComparison.OrdinalIgnoreCase);
            var fitted = MeshCompare.ReadPositions(result.FittedGlb);
            Assert.True(fitted.Count >= 12);
        }
        finally
        {
            if (Directory.Exists(work))
                Directory.Delete(work, recursive: true);
        }
    }
}

[Collection("AnnySerial")]
public class GarmentFitPresetTests
{
    public static readonly string[] Presets = ["adult-average", "tall-slim", "muscular-male"];

    [SkippableFact(Timeout = 600000)]
    public async Task JacketFitsThreeHumanPresets_WithClippingReports()
    {
        var anny = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        var gc = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        if (anny.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental) ||
            gc.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("Anny and/or GarmentCode runtime missing; preset jacket fit test not executed.");
            return;
        }

        var work = Path.Combine(Path.GetTempPath(), "3dgod-fit3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            await using var annySvc = new AnnyHumanService(new WorkerProcessHost());
            await using var garment = new GarmentCodeService(new WorkerProcessHost());
            var svc = new GarmentFitService(annySvc, garment);
            var heights = new Dictionary<string, float>();
            foreach (var name in Presets)
            {
                var result = await svc.FitJacketToPresetAsync(name, Path.Combine(work, name));
                Assert.True(File.Exists(result.FittedGlb), name + " fitted GLB missing.");
                Assert.True(File.Exists(result.ReportPath), name + " clipping report missing.");
                Assert.Equal(0, result.Report.InsideAfter);
                Assert.True(result.Report.Measurements.HeightM > 0.25f, name + " body height " + result.Report.Measurements.HeightM);
                heights[name] = result.Report.Measurements.HeightM;
            }
            Assert.True(heights["tall-slim"] > heights["adult-average"] + 0.02f,
                $"tall-slim should be taller: {heights["tall-slim"]} vs {heights["adult-average"]}");
        }
        finally
        {
            if (Directory.Exists(work))
                Directory.Delete(work, recursive: true);
        }
    }
}
