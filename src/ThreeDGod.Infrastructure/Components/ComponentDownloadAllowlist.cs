using ThreeDGod.Infrastructure.Components;

namespace ThreeDGod.Infrastructure.Components;

/// <summary>Allowlisted download hosts for component/model acquisition. No arbitrary AI URLs.</summary>
public static class ComponentDownloadAllowlist
{
    private static readonly string[] AllowedHostSuffixes =
    [
        "github.com",
        "githubusercontent.com",
        "huggingface.co",
        "hf.co",
        "astral.sh"
    ];

    public static bool IsAllowed(Uri url)
    {
        if (url.Scheme is not ("http" or "https"))
            return false;
        var host = url.Host.ToLowerInvariant();
        return AllowedHostSuffixes.Any(suffix =>
            host == suffix || host.EndsWith("." + suffix, StringComparison.Ordinal));
    }

    public static void EnsureAllowed(Uri url)
    {
        if (!IsAllowed(url))
            throw new InvalidOperationException(
                $"BlockedUrl – host '{url.Host}' is not on the component download allowlist.");
    }
}
