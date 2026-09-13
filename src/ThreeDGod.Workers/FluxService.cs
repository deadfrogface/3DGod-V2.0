using System.Text.Json;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Workers;

public sealed class FluxRuntimeStatus
{
    public FeatureAvailability Availability { get; init; } = FeatureAvailability.NotInstalled;
    public string Message { get; init; } = "";
    public string? UvPath { get; init; }
    public string? WorkerScript { get; init; }
    public string? ProjectDir { get; init; }
    public string? ModelDir { get; init; }
    public bool HasCheckpoint { get; init; }
}

public static class FluxRuntime
{
    public const string ComponentId = "flux";
    public const string HfRepo = "black-forest-labs/FLUX.1-schnell";
    public const string HfRevision = "741f7c3ce8b383c54771c7003378a50191e9efe9";
    public const int MinVramMb = 8192;
    public const long MinFreeDiskBytes = 40L * 1024 * 1024 * 1024;

    public static FluxRuntimeStatus Probe(string? repoRoot = null)
    {
        var root = repoRoot ?? AnnyRuntime.FindRepoRoot();
        var projectDir = Path.Combine(root, "workers", "flux");
        var script = Path.Combine(projectDir, "flux_worker.py");
        var lockFile = Path.Combine(projectDir, "uv.lock");
        var uv = AnnyRuntime.FindUv();
        var modelDir = GetDefaultModelDir();
        var hasCkpt = HasCheckpoint(modelDir);
        var hw = ProbeHardware();

        if (!File.Exists(script))
            return new FluxRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message = "NotInstalled – FLUX worker script missing.",
                ModelDir = modelDir,
                HasCheckpoint = hasCkpt
            };
        if (uv is null)
            return new FluxRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message = "NotInstalled – uv is not on PATH.",
                WorkerScript = script,
                ProjectDir = projectDir,
                ModelDir = modelDir,
                HasCheckpoint = hasCkpt
            };
        if (!File.Exists(lockFile) && !Directory.Exists(Path.Combine(projectDir, ".venv")))
            return new FluxRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message = "NotInstalled – FLUX uv environment is not installed. No image will be generated.",
                UvPath = uv,
                WorkerScript = script,
                ProjectDir = projectDir,
                ModelDir = modelDir,
                HasCheckpoint = hasCkpt
            };
        if (!hasCkpt)
            return new FluxRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message =
                    "NotInstalled – FLUX.1-schnell Apache-2.0 checkpoint missing. " +
                    $"Acquire HF {HfRepo}@{HfRevision} (gated:auto accept once). No image will be faked.",
                UvPath = uv,
                WorkerScript = script,
                ProjectDir = projectDir,
                ModelDir = modelDir,
                HasCheckpoint = false
            };
        if (!hw.Cuda || hw.VramMb < MinVramMb)
            return new FluxRuntimeStatus
            {
                Availability = FeatureAvailability.UnsupportedHardware,
                Message =
                    $"UnsupportedHardware – FLUX.1-schnell needs CUDA and >={MinVramMb}MB VRAM " +
                    $"(detected cuda={hw.Cuda}, vramMb={hw.VramMb}). IMPLEMENTED_GATED_HARDWARE. No image will be faked.",
                UvPath = uv,
                WorkerScript = script,
                ProjectDir = projectDir,
                ModelDir = modelDir,
                HasCheckpoint = true
            };

        return new FluxRuntimeStatus
        {
            Availability = FeatureAvailability.Experimental,
            Message = "Experimental – FLUX.1-schnell worker ready (Apache-2.0). Real PNG only via CUDA inference.",
            UvPath = uv,
            WorkerScript = script,
            ProjectDir = projectDir,
            ModelDir = modelDir,
            HasCheckpoint = true
        };
    }

    public static bool HasCheckpoint(string modelDir)
    {
        if (File.Exists(Path.Combine(modelDir, "model_index.json")) &&
            Directory.Exists(Path.Combine(modelDir, "transformer")))
            return true;
        return File.Exists(Path.Combine(modelDir, "flux1-schnell.safetensors"))
               && File.Exists(Path.Combine(modelDir, "ae.safetensors"));
    }

    public static string GetDefaultModelDir()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Models", ComponentId);
        }

        var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        var baseDir = string.IsNullOrWhiteSpace(xdg)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share")
            : xdg;
        return Path.Combine(baseDir, "3DGod", "Models", ComponentId);
    }

    private static (bool Cuda, int VramMb) ProbeHardware()
    {
        // Prefer ThreeDGod.Infrastructure.HardwareProfiler when available via env overrides for tests.
        var cudaEnv = Environment.GetEnvironmentVariable("THREEDGOD_CUDA");
        var vramEnv = Environment.GetEnvironmentVariable("THREEDGOD_VRAM_MB");
        if (string.Equals(cudaEnv, "1", StringComparison.Ordinal) && int.TryParse(vramEnv, out var forced))
            return (true, forced);

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=memory.total --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return (false, 0);
            if (!p.WaitForExit(3000))
            {
                try { p.Kill(true); } catch { /* ignore */ }
                return (false, 0);
            }
            var line = p.StandardOutput.ReadLine();
            if (p.ExitCode != 0 || string.IsNullOrWhiteSpace(line)) return (false, 0);
            return int.TryParse(line.Trim(), out var mb) ? (true, mb) : (true, 0);
        }
        catch
        {
            return (false, 0);
        }
    }
}

public sealed class FluxService : IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private readonly IDiagnosticService? _diagnostics;
    private WorkerSession? _session;

    public FluxService(IWorkerHost _, IDiagnosticService? diagnostics = null) => _diagnostics = diagnostics;

    public FluxRuntimeStatus Probe() => FluxRuntime.Probe();

    public async Task<string> GeneratePngAsync(
        string prompt,
        string destinationPng,
        long? seed = null,
        int width = 1024,
        int height = 1024,
        CancellationToken cancellationToken = default)
    {
        return await PipelineTrace.RunAsync(_diagnostics, "AI", "ReferenceImage.FLUX", async () =>
        {
            var status = FluxRuntime.Probe();
            if (status.Availability is FeatureAvailability.NotInstalled
                or FeatureAvailability.UnsupportedHardware
                or FeatureAvailability.Disabled)
                throw new InvalidOperationException(status.Message);
            if (string.IsNullOrWhiteSpace(prompt))
                throw new InvalidOperationException("InvalidRequest – prompt is required.");

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPng))!);
            var payload = JsonSerializer.Serialize(new
            {
                prompt,
                outputPath = Path.GetFullPath(destinationPng),
                modelDir = status.ModelDir,
                seed,
                width,
                height,
                steps = 4,
                guidanceScale = 0.0,
                device = "auto"
            });

            await SendAsync("text.toimage.generate", payload, cancellationToken).ConfigureAwait(false);
            if (!File.Exists(destinationPng) || new FileInfo(destinationPng).Length < 64)
                throw new InvalidOperationException("FLUX worker did not write a valid PNG. No image will be faked.");
            return destinationPng;
        }, provider: "flux").ConfigureAwait(false);
    }

    public async Task<string> AcquireModelAsync(CancellationToken cancellationToken = default)
    {
        var status = FluxRuntime.Probe();
        if (status.UvPath is null || status.WorkerScript is null || status.ProjectDir is null)
            throw new InvalidOperationException(status.Message.Contains("NotInstalled", StringComparison.Ordinal)
                ? status.Message
                : "NotInstalled – FLUX worker paths incomplete for acquire.");

        var payload = JsonSerializer.Serialize(new { modelDir = status.ModelDir ?? FluxRuntime.GetDefaultModelDir() });
        await SendAsync("text.toimage.acquire", payload, cancellationToken).ConfigureAwait(false);
        var dir = status.ModelDir ?? FluxRuntime.GetDefaultModelDir();
        if (!FluxRuntime.HasCheckpoint(dir))
            throw new InvalidOperationException("AcquireFailed – FLUX checkpoint still missing after acquire.");
        return dir;
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
            await _session.DisposeAsync();
        _sessionLock.Dispose();
    }

    private async Task<WorkerRunResult> SendAsync(string method, string jsonParams, CancellationToken cancellationToken)
    {
        var status = FluxRuntime.Probe();
        // Acquire is allowed when worker env exists even if checkpoint/hardware not ready.
        var allowAcquire = method is "text.toimage.acquire" or "flux.acquire";
        if (!allowAcquire &&
            status.Availability is FeatureAvailability.NotInstalled
                or FeatureAvailability.UnsupportedHardware
                or FeatureAvailability.Disabled)
            throw new InvalidOperationException(status.Message);
        if (status.UvPath is null || status.WorkerScript is null || status.ProjectDir is null)
            throw new InvalidOperationException("NotInstalled – FLUX runtime paths incomplete.");

        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            _session ??= await WorkerSession.StartAsync(
                status.UvPath,
                ["run", "--project", status.ProjectDir, "python", "-u", status.WorkerScript],
                TimeSpan.FromMinutes(5),
                cancellationToken: cancellationToken);

            var result = await _session.RequestAsync(
                new WorkerRequest(method, jsonParams, JobId: Guid.NewGuid().ToString("N"), BackendId: "flux"),
                TimeSpan.FromHours(2),
                cancellationToken);
            if (!result.Ok)
            {
                await _session.DisposeAsync();
                _session = null;
                throw new InvalidOperationException(result.ErrorMessage ?? result.ErrorCode ?? "FLUX worker failed.");
            }
            return result;
        }
        catch
        {
            if (_session is not null)
            {
                await _session.DisposeAsync();
                _session = null;
            }
            throw;
        }
        finally
        {
            _sessionLock.Release();
        }
    }
}
