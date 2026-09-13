using ThreeDGod.Application;
using ThreeDGod.Infrastructure;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

public class TripoSrStage12Tests
{
    [Fact]
    public void Probe_WithoutCheckpoint_IsNotInstalled()
    {
        var previous = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var temp = Path.Combine(Path.GetTempPath(), "3dgod-triposr-probe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", temp);
            var status = TripoSrRuntime.Probe();
            Assert.Equal(FeatureAvailability.NotInstalled, status.Availability);
            Assert.Contains("NotInstalled", status.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", previous);
            try { Directory.Delete(temp, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void ImageTo3D_TripoSr_Probe_DoesNotClaimSuccessWithoutInstall()
    {
        var previous = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var temp = Path.Combine(Path.GetTempPath(), "3dgod-triposr-img-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", temp);
            var svc = new ImageTo3DService();
            Assert.NotEqual(FeatureAvailability.Available, svc.Probe("triposr"));
            Assert.DoesNotContain("Success", svc.ProbeMessage("triposr"), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", previous);
            try { Directory.Delete(temp, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void WorkerTree_HasLockAndVendoredTsr()
    {
        var repo = RepoPaths.FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(repo, "workers", "triposr", "triposr_worker.py")));
        Assert.True(File.Exists(Path.Combine(repo, "workers", "triposr", "uv.lock")));
        Assert.True(File.Exists(Path.Combine(repo, "workers", "triposr", "tsr", "system.py")));
        Assert.True(File.Exists(Path.Combine(repo, "workers", "triposr", "component.manifest.json")));
        Assert.Contains(
            TripoSrRuntime.ModelSha256,
            File.ReadAllText(Path.Combine(repo, "workers", "triposr", "manifest.json")),
            StringComparison.OrdinalIgnoreCase);
    }
}
