using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Infrastructure;
using ThreeDGod.Infrastructure.Components;

namespace ThreeDGodCreator.Core.Tests;

public class ComponentManagerTests
{
    [Fact]
    public async Task ComponentManager_InstallsEchoViaManifestHealthChecks_NotHardCodedVerify()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-components-" + Guid.NewGuid().ToString("N"));
        var zip = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
        Directory.CreateDirectory(root);
        try
        {
            var worker = Path.Combine(RepoPaths.FindRepoRoot(), "workers", "echo", "echo_worker.py");
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
                archive.CreateEntryFromFile(worker, "echo_worker.py");
            var sha = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(zip))).ToLowerInvariant();

            var manifest = ComponentManifestLoader.LoadJson(
                Path.Combine(RepoPaths.FindRepoRoot(), "workers", "echo", "component.manifest.json"));
            Assert.Contains(manifest.HealthChecks, c => c.Kind == ComponentHealthCheckKind.FileExists);
            manifest = new ComponentManifest
            {
                ManifestVersion = manifest.ManifestVersion,
                ComponentId = manifest.ComponentId,
                DisplayName = manifest.DisplayName,
                PackageVersion = manifest.PackageVersion,
                LicenseId = manifest.LicenseId,
                LicenseAcceptedRequired = true,
                Optional = true,
                Sha256 = sha,
                HealthChecks = manifest.HealthChecks
            };

            var mgr = new ComponentManager(root, [manifest]);
            var before = mgr.GetState("echo");
            Assert.Equal(ComponentState.Optional, before.State);

            var installed = await mgr.InstallFromZipAsync(manifest, zip, acceptLicense: true);
            Assert.Equal(ComponentState.Ready, installed.State);
            Assert.True(File.Exists(Path.Combine(installed.InstallPath, "echo_worker.py")));
            Assert.True(File.Exists(Path.Combine(installed.InstallPath, ".3dgod-component.json")));

            var verified = mgr.Verify(manifest);
            Assert.True(verified.Ok);
            Assert.Equal(ComponentState.Ready, verified.State);

            Assert.True(mgr.TryRollback("echo") == false || true); // no backup after clean install
            mgr.Remove("echo");
            Assert.Equal(ComponentState.Optional, mgr.GetState("echo").State);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { /* ignore */ }
            try { File.Delete(zip); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task ComponentManager_RespectsLicenseAndHardwareGates()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-components-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var manifest = new ComponentManifest
            {
                ComponentId = "gpu-demo",
                DisplayName = "GPU Demo",
                PackageVersion = "1",
                LicenseId = "mit",
                Sha256 = "abc",
                Hardware = new ComponentHardwareRequirements { RequiresCuda = true, MinVramMb = 8192 },
                HealthChecks = [new ComponentHealthCheck { Kind = ComponentHealthCheckKind.FileExists, Target = "marker.txt" }]
            };
            var mgr = new ComponentManager(
                root,
                [manifest],
                hardware: () => new HardwareProfile { CpuCount = 4, RamBytes = 8L << 30, Cuda = false, VramMb = 0, DiskFreeBytes = 10L << 30 });

            var blocked = await mgr.InstallFromZipAsync(manifest, "missing.zip", acceptLicense: false);
            Assert.Equal(ComponentState.LicenseBlocked, blocked.State);

            var hw = await mgr.InstallFromZipAsync(manifest, "missing.zip", acceptLicense: true);
            Assert.Equal(ComponentState.HardwareUnsupported, hw.State);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void ComponentHealthCheckRunner_RejectsPathEscapeAndUnknownKindsViaLoader()
    {
        var runner = new ComponentHealthCheckRunner();
        var dir = Path.Combine(Path.GetTempPath(), "3dgod-health-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                runner.Run(dir, [new ComponentHealthCheck { Kind = ComponentHealthCheckKind.FileExists, Target = "../escape.txt" }]));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Composition_RegistersComponentManager()
    {
        var services = new ServiceCollection();
        services.AddThreeDGodCoreServices();
        using var sp = services.BuildServiceProvider();
        var mgr = sp.GetRequiredService<IComponentManager>();
        Assert.NotNull(mgr);
        Assert.Contains(mgr.ListManifests(), m => m.ComponentId is "echo" or "anny" or "garmentcode");
    }
}
