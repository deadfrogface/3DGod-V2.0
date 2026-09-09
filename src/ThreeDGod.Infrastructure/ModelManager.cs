using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ThreeDGod.Persistence;

namespace ThreeDGod.Infrastructure;

public sealed class ModelPackageRecord
{
    public string Id { get; init; } = "";
    public string Version { get; init; } = "";
    public string InstallPath { get; init; } = "";
    public string Sha256 { get; init; } = "";
    public bool LicenseAccepted { get; init; }
}

public sealed class ModelManager
{
    private static readonly Regex PackageIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly string _root;
    private readonly ArchiveLimits _limits;

    public ModelManager(string root, ArchiveLimits? limits = null)
    {
        _root = Path.GetFullPath(root);
        _limits = limits ?? new ArchiveLimits();
        Directory.CreateDirectory(_root);
    }

    public static void ValidatePackageId(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            throw new InvalidOperationException("InvalidPackageId");
        if (!PackageIdPattern.IsMatch(packageId))
            throw new InvalidOperationException("InvalidPackageId");
        if (!string.Equals(packageId, Path.GetFileName(packageId), StringComparison.Ordinal))
            throw new InvalidOperationException("InvalidPackageId");
    }

    public async Task<ModelPackageRecord> InstallAsync(string packageId, string sourceZip, string expectedSha256, bool acceptLicense, CancellationToken cancellationToken = default)
    {
        ValidatePackageId(packageId);
        if (!acceptLicense)
            throw new InvalidOperationException("LicenseBlocked");
        if (!File.Exists(sourceZip))
            throw new FileNotFoundException("Model package zip missing.", sourceZip);

        await using var fs = File.OpenRead(sourceZip);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(fs, cancellationToken)).ToLowerInvariant();
        if (!string.Equals(hash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("HashMismatch");

        var dest = Path.Combine(_root, packageId);
        var destRoot = Path.GetFullPath(dest);
        if (!destRoot.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(destRoot, _root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PathEscape");

        var temp = dest + ".partial";
        if (Directory.Exists(temp))
            Directory.Delete(temp, true);
        SafeZipExtractor.ExtractToDirectory(sourceZip, temp, _limits);
        if (Directory.Exists(dest))
            Directory.Delete(dest, true);
        Directory.Move(temp, dest);
        return new ModelPackageRecord
        {
            Id = packageId,
            Version = "1",
            InstallPath = dest,
            Sha256 = hash,
            LicenseAccepted = true
        };
    }

    public bool Verify(ModelPackageRecord record)
    {
        if (!Directory.Exists(record.InstallPath))
            return false;
        var marker = Path.Combine(record.InstallPath, "echo_worker.py");
        return File.Exists(marker);
    }

    public Task<ModelPackageRecord> RepairAsync(string packageId, string sourceZip, string expectedSha256, CancellationToken cancellationToken = default) =>
        InstallAsync(packageId, sourceZip, expectedSha256, acceptLicense: true, cancellationToken);

    public void Remove(string packageId)
    {
        ValidatePackageId(packageId);
        var dest = Path.Combine(_root, packageId);
        if (Directory.Exists(dest))
            Directory.Delete(dest, true);
    }
}
