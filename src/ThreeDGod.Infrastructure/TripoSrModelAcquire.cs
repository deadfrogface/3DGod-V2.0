using ThreeDGod.Infrastructure.Components;
using ThreeDGod.Workers;

namespace ThreeDGod.Infrastructure;

/// <summary>
/// Acquires the MIT TripoSR checkpoint (config.yaml + model.ckpt) with SHA-256 verification.
/// No pipe-to-shell; uses <see cref="IComponentDownloadService"/>.
/// </summary>
public static class TripoSrModelAcquire
{
    public static async Task<string> EnsureAsync(
        IComponentDownloadService downloads,
        string? modelDir = null,
        IProgress<ComponentDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var dir = modelDir ?? TripoSrRuntime.GetDefaultModelDir();
        Directory.CreateDirectory(dir);
        var configPath = Path.Combine(dir, "config.yaml");
        var weightPath = Path.Combine(dir, "model.ckpt");

        if (!File.Exists(configPath))
        {
            await downloads.DownloadAsync(new ComponentDownloadRequest
            {
                Url = new Uri(TripoSrRuntime.ConfigDownloadUrl),
                DestinationPath = configPath,
                AllowResume = true,
                Progress = progress
            }, cancellationToken).ConfigureAwait(false);
        }

        if (!File.Exists(weightPath) || new FileInfo(weightPath).Length != TripoSrRuntime.ModelBytes)
        {
            var result = await downloads.DownloadAsync(new ComponentDownloadRequest
            {
                Url = new Uri(TripoSrRuntime.ModelDownloadUrl),
                DestinationPath = weightPath,
                ExpectedSha256 = TripoSrRuntime.ModelSha256,
                AllowResume = true,
                Progress = progress
            }, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(result.Sha256, TripoSrRuntime.ModelSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("TripoSR checkpoint SHA-256 mismatch after download.");
        }

        return dir;
    }
}
