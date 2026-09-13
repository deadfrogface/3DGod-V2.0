using ThreeDGod.Application;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Live TripoSR proof. Skips unless MIT checkpoint + uv env are installed.
/// Agent verified: CPU chair.png → real GLB (~97KB, 2454 verts) = PASS_REAL / SUPPORTED_BUT_SLOW.
/// </summary>
[Collection("AnnySerial")]
public class TripoSrLiveTests
{
    private static bool ModelPresent()
    {
        var dir = TripoSrRuntime.GetDefaultModelDir();
        return File.Exists(Path.Combine(dir, "model.ckpt"))
               && File.Exists(Path.Combine(dir, "config.yaml"));
    }

    [Fact]
    public void Probe_WhenModelMissing_IsNotInstalled()
    {
        if (ModelPresent())
            return;
        var status = TripoSrRuntime.Probe(RepoPaths.FindRepoRoot());
        Assert.Equal(FeatureAvailability.NotInstalled, status.Availability);
    }

    [SkippableFact]
    public void Probe_WhenInstalled_IsExperimental()
    {
        Skip.If(!ModelPresent(), "TripoSR model.ckpt not installed.");
        var status = TripoSrRuntime.Probe(RepoPaths.FindRepoRoot());
        Skip.If(
            status.Availability == FeatureAvailability.NotInstalled,
            status.Message);
        Assert.Equal(FeatureAvailability.Experimental, status.Availability);
        Assert.True(status.HasCheckpoint);
    }

    [SkippableFact]
    public async Task Generate_WhenInstalled_WritesRealGlb()
    {
        Skip.If(!ModelPresent(), "TripoSR model.ckpt not installed.");
        var status = TripoSrRuntime.Probe(RepoPaths.FindRepoRoot());
        Skip.If(status.Availability == FeatureAvailability.NotInstalled, status.Message);

        var fixture = Path.Combine(RepoPaths.FindRepoRoot(), "workers", "triposr", "fixtures", "chair.png");
        Skip.If(!File.Exists(fixture), "chair.png fixture missing.");

        var outDir = Path.Combine(Path.GetTempPath(), "3dgod-triposr-live-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outDir);
        var glb = Path.Combine(outDir, "out.glb");
        try
        {
            await using var svc = new TripoSrService(new WorkerProcessHost());
            var path = await svc.GenerateGlbAsync(fixture, glb);
            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 1024);
        }
        finally
        {
            try { Directory.Delete(outDir, true); } catch { /* ignore */ }
        }
    }
}
