using System.IO.Compression;

namespace ThreeDGod.Persistence;

public static class SafeZipExtractor
{
    public static void ExtractToDirectory(string zipPath, string destinationDirectory, ArchiveLimits? limits = null)
    {
        limits ??= new ArchiveLimits();
        Directory.CreateDirectory(destinationDirectory);
        var destRoot = Path.GetFullPath(destinationDirectory);
        if (!destRoot.EndsWith(Path.DirectorySeparatorChar))
            destRoot += Path.DirectorySeparatorChar;

        using var archive = ZipFile.OpenRead(zipPath);
        if (archive.Entries.Count > limits.MaxFileCount)
            throw new ProjectArchiveException("TooManyFiles", $"Archive has {archive.Entries.Count} files; max is {limits.MaxFileCount}.");

        long total = 0;
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith('/'))
                continue;

            var relative = ArchivePathRules.NormalizeRelativePath(entry.FullName);
            var destPath = Path.GetFullPath(Path.Combine(destinationDirectory, relative));
            if (!destPath.StartsWith(destRoot, StringComparison.OrdinalIgnoreCase))
                throw new ProjectArchiveException("ZipSlip", $"Extract path escapes destination: {relative}");

            if (entry.Length > limits.MaxSingleEntryBytes)
                throw new ProjectArchiveException("EntryTooLarge", $"Entry {relative} exceeds max size.");

            total += entry.Length;
            if (total > limits.MaxDecompressedBytes)
                throw new ProjectArchiveException("ArchiveTooLarge", "Decompressed archive exceeds limit.");

            var parent = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            entry.ExtractToFile(destPath, overwrite: true);
        }
    }
}
