using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ThreeDGod.AI;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

public class SecurityHardeningTests
{
    [Fact]
    public void Malicious3dgod_HashMismatch_IsRejected()
    {
        var archive = new GodProjectArchive();
        var ex = Assert.Throws<ProjectArchiveException>(() =>
        {
            using var ms = BuildManifestZip(
                manifestFiles: [new ManifestFile("project.json", "{}")],
                tamperPath: "project.json",
                tamperContent: """{"name":"evil"}""");
            archive.ReadBundle(ms);
        });
        Assert.Equal("HashMismatch", ex.Code);
    }

    [Fact]
    public void Malicious3dgod_UnknownFormat_IsRejected()
    {
        var manifest = """{"format":"zip-bomb","formatVersion":1,"projectId":"00000000-0000-0000-0000-000000000001","rootProjectPath":"project.json","zip64":true,"files":[]}""";
        var archive = new GodProjectArchive();
        var ex = Assert.Throws<ProjectArchiveException>(() =>
        {
            using var ms = BuildZip(("manifest.json", manifest), ("project.json", "{}"));
            archive.ReadBundle(ms);
        });
        Assert.Equal("UnknownFormat", ex.Code);
    }

    [Fact]
    public void DecompressionBomb_ExceedsLimit_IsRejected()
    {
        var archive = new GodProjectArchive(new ArchiveLimits
        {
            MaxDecompressedBytes = 4096,
            MaxSingleEntryBytes = 2048,
            MaxFileCount = 16
        });
        var ex = Assert.Throws<ProjectArchiveException>(() =>
        {
            using var ms = BuildManifestZip(
            [
                new ManifestFile("project.json", new string('A', 2048)),
                new ManifestFile("extra.json", new string('B', 2048))
            ]);
            archive.ReadBundle(ms);
        });
        Assert.Equal("ArchiveTooLarge", ex.Code);
    }

    [Fact]
    public void BrokenGlb_IsRejectedSafely()
    {
        var path = Path.Combine(Path.GetTempPath(), $"broken-{Guid.NewGuid():N}.glb");
        try
        {
            File.WriteAllText(path, "not-a-glb");
            var ex = Assert.Throws<GlbLoadException>(() => CanonicalGltfPipeline.Load(path));
            Assert.Equal("GlbMalformed", ex.Code);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void OversizedGlb_IsRejected()
    {
        var path = Path.Combine(Path.GetTempPath(), $"big-{Guid.NewGuid():N}.glb");
        try
        {
            File.WriteAllBytes(path, new byte[32]);
            var ex = Assert.Throws<GlbLoadException>(() =>
                CanonicalGltfPipeline.Load(path, new GlbLoadLimits { MaxFileBytes = 8 }));
            Assert.Equal("GlbTooLarge", ex.Code);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task InvalidWorkerProtocol_DoesNotThrowToCaller()
    {
        var host = new WorkerProcessHost();
        var script = """
import sys
sys.stdout.write('{"v":"3dgod-worker/1","type":"hello","worker":"echo"}\n')
sys.stdout.flush()
sys.stdout.write('{"v":"3dgod-worker/1","type":"not-result","id":"x"}\n')
sys.stdout.flush()
import time
time.sleep(2)
""";
        var result = await host.RunAsync("python", ["-u", "-c", script], new WorkerRequest("echo", "{}"), TimeSpan.FromMilliseconds(600));
        Assert.False(result.Ok);
        Assert.True(result.TimedOut || result.ErrorCode is "Timeout" or "Crash");
    }

    [Fact]
    public void PathEscape_InArchivePathRules_IsRejected()
    {
        Assert.Throws<ProjectArchiveException>(() => ArchivePathRules.NormalizeRelativePath("foo/../../etc/passwd"));
        Assert.Throws<ProjectArchiveException>(() => ArchivePathRules.NormalizeRelativePath("payload.exe"));
    }

    [Fact]
    public void ShellCharsInPrompt_AreRejected()
    {
        var plan = DeterministicAiParser.Parse("rat head; del /f /q C:\\Windows\\*");
        Assert.Equal("Unsupported", plan.Status);
        Assert.Contains("shell", plan.Reason ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShellCharsInPlanArgs_AreRejectedByValidator()
    {
        var plan = AiEditPlanValidator.Validate(new AiEditPlan
        {
            Status = "valid",
            Operation = "parameter.delta",
            Args = new Dictionary<string, string> { ["key"] = "height", ["delta"] = "0.1; calc" }
        });
        Assert.Equal("Unsupported", plan.Status);
    }

    [Fact]
    public void MaliciousModelManifest_PathTraversalDownloadUrl_IsRejected()
    {
        var manifest = new WorkerPackageManifest
        {
            ManifestVersion = 1,
            WorkerId = "evil",
            DisplayName = "Evil",
            Status = "Available",
            PackageVersion = "1.0.0",
            LicenseId = "mit",
            ReleasePackage = new WorkerReleasePackage
            {
                Format = "zip",
                DownloadUrl = "file:///etc/passwd"
            }
        };
        var result = WorkerPackageManifestReader.Validate(manifest, RepoPaths.FindRepoRoot());
        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("downloadUrl", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MaliciousModelManifest_TraversalWorkerId_IsRejected()
    {
        var manifest = new WorkerPackageManifest
        {
            ManifestVersion = 1,
            WorkerId = "../anny",
            DisplayName = "Traversal",
            Status = "Available",
            PackageVersion = "1.0.0",
            LicenseId = "mit"
        };
        var result = WorkerPackageManifestReader.Validate(manifest, RepoPaths.FindRepoRoot());
        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Contains("workerId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ModelManager_ZipSlipPackage_DoesNotEscapeRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-sec-" + Guid.NewGuid().ToString("N"));
        var zip = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
        var outside = Path.Combine(Path.GetTempPath(), "3dgod-escape-" + Guid.NewGuid().ToString("N") + ".txt");
        Directory.CreateDirectory(root);
        try
        {
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("../escape.txt", CompressionLevel.NoCompression);
                using var s = entry.Open();
                s.Write(Encoding.UTF8.GetBytes("evil") );
            }
            var sha = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(zip))).ToLowerInvariant();
            var mgr = new ModelManager(root);
            await Assert.ThrowsAsync<ProjectArchiveException>(() =>
                mgr.InstallAsync("echo", zip, sha, acceptLicense: true));
            Assert.False(File.Exists(outside));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
            try { File.Delete(zip); } catch { }
            try { if (File.Exists(outside)) File.Delete(outside); } catch { }
        }
    }

    [Fact]
    public void ModelManager_InvalidPackageId_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => ModelManager.ValidatePackageId("../anny"));
        Assert.Throws<InvalidOperationException>(() => ModelManager.ValidatePackageId("bad id"));
    }

    [Fact]
    public void WorkerHost_BlocksShellExecutable()
    {
        Assert.Throws<ArgumentException>(() => WorkerProcessHost.ValidateExecutable("powershell.exe"));
        Assert.Throws<ArgumentException>(() => WorkerProcessHost.ValidateExecutable("cmd.exe"));
    }

    [Fact]
    public async Task WorkerCrashLoop_DoesNotTakeDownHost()
    {
        var host = new WorkerProcessHost();
        var script = Path.Combine(RepoPaths.FindRepoRoot(), "workers", "echo", "echo_worker.py");
        for (var i = 0; i < 5; i++)
        {
            var result = await host.RunAsync("python", ["-u", script], new WorkerRequest("crash", "{}"), TimeSpan.FromSeconds(5));
            Assert.False(result.Ok);
        }

        var ok = await host.RunAsync("python", ["-u", script], new WorkerRequest("echo", """{"n":1}"""), TimeSpan.FromSeconds(10));
        Assert.True(ok.Ok, ok.ErrorMessage);
    }

    [Fact]
    public void SafeZipExtractor_RejectsAbsoluteEntry()
    {
        var zip = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-safe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dest);
        try
        {
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("/etc/passwd", CompressionLevel.NoCompression);
                using var s = entry.Open();
                s.Write(Encoding.UTF8.GetBytes("x"));
            }
            Assert.Throws<ProjectArchiveException>(() => SafeZipExtractor.ExtractToDirectory(zip, dest));
        }
        finally
        {
            try { File.Delete(zip); } catch { }
            try { Directory.Delete(dest, true); } catch { }
        }
    }

    private sealed record ManifestFile(string Path, string Content);

    private static MemoryStream BuildManifestZip(IReadOnlyList<ManifestFile> manifestFiles, string? tamperPath = null, string? tamperContent = null)
    {
        var files = manifestFiles.Select(f =>
        {
            var bytes = Encoding.UTF8.GetBytes(f.Content);
            return new { f.Path, Bytes = bytes, Hash = ArchivePathRules.Sha256Hex(bytes) };
        }).ToList();

        var manifest = new
        {
            format = GodProjectArchive.FormatName,
            formatVersion = GodProjectArchive.CurrentFormatVersion,
            projectId = Guid.NewGuid(),
            rootProjectPath = "project.json",
            zip64 = true,
            files = files.Select(f => new { path = f.Path, sha256 = f.Hash, size = f.Bytes.Length }).ToArray()
        };
        var manifestJson = JsonSerializer.Serialize(manifest);
        var entries = files.Select(f => (f.Path, Encoding.UTF8.GetString(f.Bytes))).ToList();
        if (tamperPath is not null && tamperContent is not null)
            entries.Add((tamperPath, tamperContent));
        entries.Add(("manifest.json", manifestJson));
        return BuildZip(entries.ToArray());
    }

    private static MemoryStream BuildZip(params (string Path, string Content)[] entries)
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in entries)
            {
                var entry = zip.CreateEntry(path, CompressionLevel.NoCompression);
                using var s = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(content);
                s.Write(bytes, 0, bytes.Length);
            }
        }
        ms.Position = 0;
        return ms;
    }
}
