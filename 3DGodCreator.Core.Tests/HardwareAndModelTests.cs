using System.IO.Compression;
using System.Security.Cryptography;
using ThreeDGod.Infrastructure;

namespace ThreeDGodCreator.Core.Tests;

public class HardwareAndModelTests
{
    [Fact]
    public async Task GpuScheduler_DoesNotRunTwoHeavyJobsInParallel()
    {
        var scheduler = new GpuJobScheduler();
        var running = 0;
        var max = 0;
        async Task<int> Job(CancellationToken ct)
        {
            var now = Interlocked.Increment(ref running);
            Interlocked.Exchange(ref max, Math.Max(max, now));
            await Task.Delay(80, ct);
            Interlocked.Decrement(ref running);
            return now;
        }

        await Task.WhenAll(
            scheduler.RunHeavyAsync(Job),
            scheduler.RunHeavyAsync(Job));
        Assert.Equal(1, max);
    }

    [Fact]
    public void HardwareProfiler_ReturnsCpuRamAndDisk()
    {
        var profile = HardwareProfiler.Probe();
        Assert.True(profile.CpuCount > 0);
        Assert.True(profile.RamBytes > 0);
        Assert.True(profile.DiskFreeBytes >= 0);
    }

    [Fact]
    public async Task ModelManager_InstallVerifyRunRemove_DemoEchoPackage()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-models-" + Guid.NewGuid().ToString("N"));
        var zip = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
        Directory.CreateDirectory(root);
        try
        {
            var worker = Path.Combine(RepoPaths.FindRepoRoot(), "workers", "echo", "echo_worker.py");
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(worker, "echo_worker.py");
            }
            var sha = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(zip))).ToLowerInvariant();
            var mgr = new ModelManager(root);
            var installed = await mgr.InstallAsync("echo", zip, sha, acceptLicense: true);
            Assert.True(mgr.Verify(installed));
            Assert.True(File.Exists(Path.Combine(installed.InstallPath, "echo_worker.py")));
            mgr.Remove("echo");
            Assert.False(Directory.Exists(installed.InstallPath));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
            try { File.Delete(zip); } catch { }
        }
    }

    [Fact]
    public async Task ModelManager_RejectsLicenseAndHashFailures()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-models-" + Guid.NewGuid().ToString("N"));
        var zip = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            archive.CreateEntry("dummy.txt").Open().Dispose();
        var mgr = new ModelManager(root);
        await Assert.ThrowsAsync<InvalidOperationException>(() => mgr.InstallAsync("x", zip, "deadbeef", acceptLicense: true));
        await Assert.ThrowsAsync<InvalidOperationException>(() => mgr.InstallAsync("x", zip, "deadbeef", acceptLicense: false));
        try { File.Delete(zip); } catch { }
        try { Directory.Delete(root, true); } catch { }
    }
}
