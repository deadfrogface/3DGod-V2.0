using System.Security.Cryptography;
using System.IO.Compression;

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
    private readonly string _root;

    public ModelManager(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<ModelPackageRecord> InstallAsync(string packageId, string sourceZip, string expectedSha256, bool acceptLicense, CancellationToken cancellationToken = default)
    {
        if (!acceptLicense)
            throw new InvalidOperationException("LicenseBlocked");
        await using var fs = File.OpenRead(sourceZip);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(fs, cancellationToken)).ToLowerInvariant();
        if (!string.Equals(hash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("HashMismatch");

        var dest = Path.Combine(_root, packageId);
        var temp = dest + ".partial";
        if (Directory.Exists(temp))
            Directory.Delete(temp, true);
        ZipFile.ExtractToDirectory(sourceZip, temp);
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
        var dest = Path.Combine(_root, packageId);
        if (Directory.Exists(dest))
            Directory.Delete(dest, true);
    }
}
