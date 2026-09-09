namespace ThreeDGod.Export;

/// <summary>
/// Lightweight FBX file checks. Does not claim UE5 editor import success.
/// </summary>
public static class FbxSanity
{
    public const int MinimumBytes = 64;

    private static readonly byte[] KaydaraBinaryMagic = "Kaydara FBX Binary"u8.ToArray();

    public sealed record Result(bool Passed, IReadOnlyList<string> Issues, bool HasKaydaraHeader, long FileSizeBytes);

    public static Result Check(string? path)
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            issues.Add("FBX missing – real UE5 import is not claimed.");
            return new Result(false, issues, false, 0);
        }

        var info = new FileInfo(path);
        if (info.Length < MinimumBytes)
        {
            issues.Add($"FBX too small ({info.Length} bytes) to be a valid scene.");
            return new Result(false, issues, false, info.Length);
        }

        var hasHeader = TryReadKaydaraHeader(path, out var headerKind);
        if (hasHeader)
            issues.Add($"FBX present ({headerKind}) – UE5 editor import not claimed.");
        else
            issues.Add("FBX present – header not verified; UE5 editor import not claimed.");

        return new Result(true, issues, hasHeader, info.Length);
    }

    public static bool TryReadKaydaraHeader(string path, out string headerKind)
    {
        headerKind = "";
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> prefix = stackalloc byte[32];
            var read = stream.Read(prefix);
            if (read < 8)
                return false;

            if (prefix.StartsWith(KaydaraBinaryMagic))
            {
                headerKind = "Kaydara binary";
                return true;
            }

            if (read >= 5 && prefix[0] == (byte)';')
            {
                var ascii = System.Text.Encoding.ASCII.GetString(prefix[..Math.Min(read, 20)]);
                if (ascii.Contains("FBX", StringComparison.Ordinal))
                {
                    headerKind = "ASCII FBX";
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
