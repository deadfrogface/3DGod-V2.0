using System.Diagnostics;
using System.Security.Cryptography;
using ThreeDGod.Infrastructure.Components;

namespace ThreeDGod.Infrastructure;

/// <summary>
/// Product path for skin-tokens.cpp: download pinned SkinTokens-GGUF F16 + install CLI
/// into InstallLayout (no developer CMake/Git required for model acquire; CLI comes from
/// packaged dist or a prior CI/native build placed under Components).
/// </summary>
public sealed class SkinTokensCppProvisionResult
{
    public required bool Ok { get; init; }
    public required string Message { get; init; }
    public string? CliPath { get; init; }
    public string? ModelDir { get; init; }
    public string UpstreamCommit { get; init; } = SkinTokensCppRuntime.UpstreamCommitHint;
}

public interface ISkinTokensCppProvisioner
{
    Task<SkinTokensCppProvisionResult> EnsureAsync(
        bool acceptLicense,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class SkinTokensCppProvisioner : ISkinTokensCppProvisioner
{
    public const string ModelRepo = "LocalAI-io/SkinTokens-GGUF";
    public const string ModelRevision = "main";

    private static readonly (string Name, string Sha256, long Size)[] F16Files =
    [
        ("mesh-encoder.gguf", "532710809e3db6c54389dd6489c2aa3c768244868b9c197198d06fa664214527", 57813184),
        ("skin-vae.gguf", "dcd5859ac89bae62bcfd6b7823f9d0e2393b22364bcb19f84c1109af8e07455e", 244014784),
        ("tokenrig.gguf", "933529dc0e550fd499e2e1ae1c574c6d5852047c04832e7207304e184901ed82", 948562656)
    ];

    private readonly IComponentDownloadService _downloads;

    public SkinTokensCppProvisioner(IComponentDownloadService downloads) => _downloads = downloads;

    public async Task<SkinTokensCppProvisionResult> EnsureAsync(
        bool acceptLicense,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!acceptLicense)
        {
            return new SkinTokensCppProvisionResult
            {
                Ok = false,
                Message = "LicenseBlocked – apache-2.0 acceptance required for skin-tokens.cpp."
            };
        }

        var modelDir = Path.Combine(InstallLayout.ModelsRoot, "skintokens-cpp", "F16");
        Directory.CreateDirectory(modelDir);
        foreach (var file in F16Files)
        {
            var dest = Path.Combine(modelDir, file.Name);
            if (File.Exists(dest) && new FileInfo(dest).Length == file.Size &&
                string.Equals(await Sha256FileAsync(dest, cancellationToken), file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report($"Model present: {file.Name}");
                continue;
            }

            progress?.Report($"Downloading {file.Name}…");
            var url = new Uri($"https://huggingface.co/{ModelRepo}/resolve/{ModelRevision}/F16/{file.Name}");
            await _downloads.DownloadAsync(new ComponentDownloadRequest
            {
                Url = url,
                DestinationPath = dest,
                ExpectedSha256 = file.Sha256
            }, cancellationToken);

            if (new FileInfo(dest).Length != file.Size)
            {
                return new SkinTokensCppProvisionResult
                {
                    Ok = false,
                    Message = $"HashMismatch/size – {file.Name} size {new FileInfo(dest).Length} expected {file.Size}."
                };
            }
        }

        var cli = EnsureCliInstalled(progress);
        if (cli is null)
        {
            return new SkinTokensCppProvisionResult
            {
                Ok = false,
                Message =
                    "NotInstalled – skintokens-cli missing. Models are ready; place a built CLI under " +
                    $"{InstallLayout.ComponentsRoot}/skintokens-cpp/bin (CI: Invoke-ProvisionSkinTokensCpp.ps1) " +
                    "or ship workers/skintokens-cpp/dist/bin with the installer.",
                ModelDir = modelDir
            };
        }

        progress?.Report($"CLI ready: {cli}");
        return new SkinTokensCppProvisionResult
        {
            Ok = true,
            Message = $"Ready – CLI={cli}; models={modelDir}; upstream@{SkinTokensCppRuntime.UpstreamCommitHint}",
            CliPath = cli,
            ModelDir = modelDir
        };
    }

    private static string? EnsureCliInstalled(IProgress<string>? progress)
    {
        var existing = InstallLayout.SkinTokensCppCliPath();
        if (File.Exists(existing))
            return existing;

        var content = InstallLayout.ResolveContentRoot();
        var packaged = new[]
        {
            Path.Combine(content, "workers", "skintokens-cpp", "dist", "bin", "skintokens-cli.exe"),
            Path.Combine(content, "workers", "skintokens-cpp", "dist", "bin", "skintokens-cli")
        };
        var src = packaged.FirstOrDefault(File.Exists);
        if (src is null)
            return null;

        var destDir = Path.Combine(InstallLayout.ComponentsRoot, "skintokens-cpp", "bin");
        Directory.CreateDirectory(destDir);
        progress?.Report($"Installing CLI from packaged dist → {destDir}");
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(src)!))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);

        var dest = Path.Combine(destDir, Path.GetFileName(src));
        return File.Exists(dest) ? dest : null;
    }

    private static async Task<string> Sha256FileAsync(string path, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(fs, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
