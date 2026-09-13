using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ThreeDGod.Infrastructure.Components;

public sealed class UvPin
{
    public required string Version { get; init; }
    public required string WindowsX64Url { get; init; }
    public required string WindowsX64Sha256 { get; init; }
    public required string LinuxX64Url { get; init; }
    public required string LinuxX64Sha256 { get; init; }
}

public sealed class UvProvisionResult
{
    public required string UvPath { get; init; }
    public required string Version { get; init; }
    public required string ManagedRoot { get; init; }
    public bool AlreadyPresent { get; init; }
}

public sealed class UvSyncResult
{
    public required string ProjectDir { get; init; }
    public required string VenvDir { get; init; }
    public required bool Ok { get; init; }
    public string Message { get; init; } = "";
}

public interface IUvProvisioner
{
    UvPin Pin { get; }
    string? FindManagedUv();
    Task<UvProvisionResult> EnsureUvAsync(IProgress<ComponentDownloadProgress>? progress = null, CancellationToken cancellationToken = default);
    Task<UvSyncResult> SyncWorkerAsync(string projectDir, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// WRAP of astral-sh/uv: pinned binary acquisition + hash verify + managed layout.
/// Never uses pipe-to-shell (irm | iex). Existing workers/*/uv.lock remain authoritative.
/// </summary>
public sealed class UvProvisioner : IUvProvisioner
{
    private readonly IComponentDownloadService _downloads;
    private readonly string _managedRoot;

    /// <summary>
    /// Pinned uv 0.6.16 (stable line used when Anny/Garment CI locks were proven).
    /// SHA-256 files published by astral-sh/uv releases.
    /// </summary>
    public static UvPin DefaultPin { get; } = new()
    {
        Version = "0.6.16",
        WindowsX64Url = "https://github.com/astral-sh/uv/releases/download/0.6.16/uv-x86_64-pc-windows-msvc.zip",
        WindowsX64Sha256 = "f1b4c24ec602b6c0b06b6bc64aa447bbe4cf313e93fbec326741961e80ffa8b7",
        LinuxX64Url = "https://github.com/astral-sh/uv/releases/download/0.6.16/uv-x86_64-unknown-linux-gnu.tar.gz",
        LinuxX64Sha256 = "e9ef28b675df68978a60f87192fb8c730b8bfca9bef42b121686b218ea0f6542"
    };

    public UvPin Pin { get; }

    public UvProvisioner(IComponentDownloadService downloads, string? managedRoot = null, UvPin? pin = null)
    {
        _downloads = downloads;
        Pin = pin ?? DefaultPin;
        _managedRoot = managedRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod",
            "runtime",
            "uv",
            Pin.Version);
    }

    public string? FindManagedUv()
    {
        var exe = Path.Combine(_managedRoot, OperatingSystem.IsWindows() ? "uv.exe" : "uv");
        return File.Exists(exe) ? exe : null;
    }

    public async Task<UvProvisionResult> EnsureUvAsync(
        IProgress<ComponentDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var existing = FindManagedUv();
        if (existing is not null)
        {
            return new UvProvisionResult
            {
                UvPath = existing,
                Version = Pin.Version,
                ManagedRoot = _managedRoot,
                AlreadyPresent = true
            };
        }

        Directory.CreateDirectory(_managedRoot);
        var (url, sha, archiveName) = SelectAsset();
        var archivePath = Path.Combine(_managedRoot, archiveName);
        await _downloads.DownloadAsync(new ComponentDownloadRequest
        {
            Url = new Uri(url),
            DestinationPath = archivePath,
            ExpectedSha256 = sha,
            Progress = progress
        }, cancellationToken);

        ExtractArchive(archivePath, _managedRoot);
        var uv = FindManagedUv()
            ?? throw new InvalidOperationException("UvExtractFailed – pinned archive did not contain uv binary.");

        File.WriteAllText(
            Path.Combine(_managedRoot, "pin.json"),
            JsonSerializer.Serialize(new { version = Pin.Version, sha256 = sha, url, installedUtc = DateTime.UtcNow }));

        return new UvProvisionResult
        {
            UvPath = uv,
            Version = Pin.Version,
            ManagedRoot = _managedRoot,
            AlreadyPresent = false
        };
    }

    public async Task<UvSyncResult> SyncWorkerAsync(
        string projectDir,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var fullProject = Path.GetFullPath(projectDir);
        var lockFile = Path.Combine(fullProject, "uv.lock");
        var pyproject = Path.Combine(fullProject, "pyproject.toml");
        if (!File.Exists(lockFile) || !File.Exists(pyproject))
        {
            return new UvSyncResult
            {
                ProjectDir = fullProject,
                VenvDir = Path.Combine(fullProject, ".venv"),
                Ok = false,
                Message = "Missing uv.lock or pyproject.toml – refusing to invent dependencies."
            };
        }

        var uv = await EnsureUvAsync(cancellationToken: cancellationToken);
        progress?.Report($"uv sync --frozen ({Path.GetFileName(fullProject)})");

        var psi = new ProcessStartInfo
        {
            FileName = uv.UvPath,
            Arguments = $"sync --project \"{fullProject}\" --frozen",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start managed uv.");
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var venv = Path.Combine(fullProject, ".venv");
        var ok = process.ExitCode == 0 && Directory.Exists(venv);
        return new UvSyncResult
        {
            ProjectDir = fullProject,
            VenvDir = venv,
            Ok = ok,
            Message = ok
                ? "uv sync completed."
                : $"uv sync failed (exit {process.ExitCode}): {Trim(stderr.Length > 0 ? stderr : stdout)}"
        };
    }

    private (string Url, string Sha256, string ArchiveName) SelectAsset()
    {
        if (OperatingSystem.IsWindows() && RuntimeInformation.OSArchitecture == Architecture.X64)
            return (Pin.WindowsX64Url, Pin.WindowsX64Sha256, "uv-windows-x64.zip");
        if (OperatingSystem.IsLinux() && RuntimeInformation.OSArchitecture == Architecture.X64)
            return (Pin.LinuxX64Url, Pin.LinuxX64Sha256, "uv-linux-x64.tar.gz");
        throw new InvalidOperationException(
            $"HardwareUnsupported – no pinned uv binary for {RuntimeInformation.OSDescription}/{RuntimeInformation.OSArchitecture}.");
    }

    private static void ExtractArchive(string archivePath, string destDir)
    {
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var zip = ZipFile.OpenRead(archivePath);
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;
                if (!entry.Name.Equals("uv.exe", StringComparison.OrdinalIgnoreCase) &&
                    !entry.Name.Equals("uv", StringComparison.OrdinalIgnoreCase) &&
                    !entry.Name.Equals("uvw.exe", StringComparison.OrdinalIgnoreCase))
                    continue;
                var outPath = Path.Combine(destDir, entry.Name);
                entry.ExtractToFile(outPath, overwrite: true);
            }
            return;
        }

        // tar.gz: extract with tar when available (Linux agents / CI).
        var psi = new ProcessStartInfo
        {
            FileName = "tar",
            Arguments = $"-xzf \"{archivePath}\" -C \"{destDir}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("tar is required to extract uv on this platform.");
        process.WaitForExit(120_000);
        if (process.ExitCode != 0)
            throw new InvalidOperationException("tar extract of uv archive failed.");

        // Astral archives nest binary under uv-x86_64-.../uv — flatten.
        foreach (var candidate in Directory.EnumerateFiles(destDir, "uv", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(destDir, "uv.exe", SearchOption.AllDirectories)))
        {
            var target = Path.Combine(destDir, Path.GetFileName(candidate));
            if (!string.Equals(candidate, target, StringComparison.Ordinal))
                File.Copy(candidate, target, overwrite: true);
        }
    }

    private static string Trim(string s) =>
        s.Length <= 800 ? s.Trim() : s.Trim()[..800] + "…";
}
