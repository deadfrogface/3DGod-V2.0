using System.Diagnostics;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;

namespace ThreeDGod.Infrastructure;

public static class SkinTokensCppRuntime
{
    public const string WorkerId = "skintokens-cpp";
    public const string LicenseId = "apache-2.0";
    public const string UpstreamRepo = "https://github.com/localai-org/skin-tokens.cpp";
    public const string UpstreamCommitHint = "43e885af2eadee9c40aa85849b71528d1c958293";
    public const string ModelRepo = "https://huggingface.co/LocalAI-io/SkinTokens-GGUF";

    public static GatedWorkerStatus Probe(string device = "cpu")
    {
        var cli = InstallLayout.SkinTokensCppCliPath();
        if (!File.Exists(cli))
        {
            return new GatedWorkerStatus
            {
                WorkerId = WorkerId,
                Availability = FeatureAvailability.NotInstalled,
                Message =
                    $"NotInstalled – skin-tokens.cpp CLI missing ({cli}). " +
                    $"Apache-2.0 upstream {UpstreamRepo}@{UpstreamCommitHint}. " +
                    "Install via Setup Assistant. No rig will be faked."
            };
        }

        var model = InstallLayout.FindSkinTokensCppModelDir();
        if (string.IsNullOrWhiteSpace(model))
        {
            return new GatedWorkerStatus
            {
                WorkerId = WorkerId,
                Availability = FeatureAvailability.NotInstalled,
                Message =
                    "NotInstalled – SkinTokens-GGUF F16 model dir missing. " +
                    $"Acquire from {ModelRepo} (MIT-labelled weights via LocalAI GGUF). No rig will be faked."
            };
        }

        var hw = HardwareProfiler.Probe();
        if (string.Equals(device, "vulkan", StringComparison.OrdinalIgnoreCase) && !hw.Vulkan)
        {
            return new GatedWorkerStatus
            {
                WorkerId = WorkerId,
                Availability = FeatureAvailability.UnsupportedHardware,
                Message =
                    "UnsupportedHardware – Vulkan not detected for skin-tokens.cpp. " +
                    "IMPLEMENTED_GATED_VULKAN_RUNTIME_PROOF until Vulkan runtime proven. CPU path may still be available."
            };
        }

        return new GatedWorkerStatus
        {
            WorkerId = WorkerId,
            Availability = FeatureAvailability.Experimental,
            Message =
                $"Experimental – skin-tokens.cpp ready ({device}). CLI={cli}; model={model}. " +
                "Real skinned GLB only via CLI inference."
        };
    }

    public static async Task<string> RigAsync(
        string sourceGlb,
        string destinationGlb,
        string device,
        IDiagnosticService? diagnostics = null,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(destinationGlb))
            File.Delete(destinationGlb);

        return await PipelineTrace.RunAsync(diagnostics, "Rig", "SkinTokensCpp.Rig", async () =>
        {
            var status = Probe(device);
            if (status.Availability is FeatureAvailability.NotInstalled
                or FeatureAvailability.UnsupportedHardware
                or FeatureAvailability.Disabled)
                throw new InvalidOperationException(status.Message);

            var cli = InstallLayout.SkinTokensCppCliPath();
            var model = InstallLayout.FindSkinTokensCppModelDir()
                        ?? throw new InvalidOperationException("SkinTokens-GGUF model dir missing.");

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationGlb))!);
            var psi = new ProcessStartInfo
            {
                FileName = cli,
                ArgumentList = { "rig", model, sourceGlb, destinationGlb, "--device", device },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start skintokens-cli.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            if (process.ExitCode != 0 || !File.Exists(destinationGlb) || new FileInfo(destinationGlb).Length < 64)
            {
                throw new InvalidOperationException(
                    $"skin-tokens.cpp rig failed (exit {process.ExitCode}). stderr={Trim(stderr)} stdout={Trim(stdout)}");
            }

            return destinationGlb;
        }, provider: "skintokens-cpp");
    }

    private static string Trim(string s) =>
        string.IsNullOrWhiteSpace(s) ? "" : (s.Length <= 800 ? s : s[..800] + "…");
}
