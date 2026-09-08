using System.Security.Cryptography;
using System.Text;

namespace ThreeDGod.Persistence;

public sealed class ArchiveLimits
{
    public int MaxFileCount { get; init; } = 10_000;
    public long MaxSingleEntryBytes { get; init; } = 512L * 1024 * 1024;
    public long MaxDecompressedBytes { get; init; } = 2L * 1024 * 1024 * 1024;
}

public sealed class ProjectArchiveException : Exception
{
    public string Code { get; }

    public ProjectArchiveException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public static class ArchivePathRules
{
    private static readonly HashSet<string> ForbiddenExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".ps1", ".com", ".scr", ".msi", ".sh"
    };

    public static string NormalizeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ProjectArchiveException("EmptyPath", "Archive entry path is empty.");

        var normalized = path.Replace('\\', '/').Trim();
        if (normalized.StartsWith('/'))
            throw new ProjectArchiveException("AbsolutePath", $"Absolute archive path rejected: {path}");

        if (normalized.Contains(':') || Path.IsPathRooted(path) || Path.IsPathFullyQualified(path))
            throw new ProjectArchiveException("AbsolutePath", $"Absolute archive path rejected: {path}");

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s == "." || s == ".."))
            throw new ProjectArchiveException("ZipSlip", $"Unsafe relative path rejected: {path}");

        var joined = string.Join('/', segments);
        var ext = Path.GetExtension(joined);
        if (ForbiddenExtensions.Contains(ext))
            throw new ProjectArchiveException("ExecutableRejected", $"Executable entries are not allowed: {joined}");

        return joined;
    }

    public static string Sha256Hex(ReadOnlySpan<byte> bytes)
    {
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
