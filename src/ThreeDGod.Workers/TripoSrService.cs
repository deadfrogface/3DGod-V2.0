using System.Text.Json;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;

namespace ThreeDGod.Workers;

public sealed class TripoSrRuntimeStatus
{
    public FeatureAvailability Availability { get; init; } = FeatureAvailability.NotInstalled;
    public string Message { get; init; } = "";
    public string? UvPath { get; init; }
    public string? WorkerScript { get; init; }
    public string? ProjectDir { get; init; }
    public string? ModelDir { get; init; }
    public bool HasCheckpoint { get; init; }
}

public static class TripoSrRuntime
{
    public const string ComponentId = "triposr";
    public const string ModelSha256 = "429e2c6b22a0923967459de24d67f05962b235f79cde6b032aa7ed2ffcd970ee";
    public const long ModelBytes = 1_677_246_742;
    public const string ModelDownloadUrl = "https://huggingface.co/stabilityai/TripoSR/resolve/main/model.ckpt";
    public const string ConfigDownloadUrl = "https://huggingface.co/stabilityai/TripoSR/resolve/main/config.yaml";

    public static TripoSrRuntimeStatus Probe(string? repoRoot = null)
    {
        var root = repoRoot ?? AnnyRuntime.FindRepoRoot();
        var projectDir = Path.Combine(root, "workers", "triposr");
        var script = Path.Combine(projectDir, "triposr_worker.py");
        var lockFile = Path.Combine(projectDir, "uv.lock");
        var uv = AnnyRuntime.FindUv();
        var modelDir = GetDefaultModelDir();
        var hasCkpt = File.Exists(Path.Combine(modelDir, "model.ckpt"))
                      && File.Exists(Path.Combine(modelDir, "config.yaml"));

        if (!File.Exists(script))
            return new TripoSrRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message = "NotInstalled – TripoSR worker script missing.",
                ModelDir = modelDir,
                HasCheckpoint = hasCkpt
            };
        if (uv is null)
            return new TripoSrRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message = "NotInstalled – uv is not on PATH.",
                WorkerScript = script,
                ProjectDir = projectDir,
                ModelDir = modelDir,
                HasCheckpoint = hasCkpt
            };
        if (!File.Exists(lockFile) && !Directory.Exists(Path.Combine(projectDir, ".venv")))
            return new TripoSrRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message = "NotInstalled – TripoSR uv environment is not installed. No mesh will be generated.",
                UvPath = uv,
                WorkerScript = script,
                ProjectDir = projectDir,
                ModelDir = modelDir,
                HasCheckpoint = hasCkpt
            };
        if (!hasCkpt)
            return new TripoSrRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message = "NotInstalled – TripoSR MIT checkpoint (model.ckpt) is not installed. Acquire via Setup Assistant / model download.",
                UvPath = uv,
                WorkerScript = script,
                ProjectDir = projectDir,
                ModelDir = modelDir,
                HasCheckpoint = false
            };

        return new TripoSrRuntimeStatus
        {
            Availability = FeatureAvailability.Experimental,
            Message = "Experimental – TripoSR WRAP via uv. CPU supported but slow; CUDA recommended (~6GB).",
            UvPath = uv,
            WorkerScript = script,
            ProjectDir = projectDir,
            ModelDir = modelDir,
            HasCheckpoint = true
        };
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
}

public sealed class TripoSrService : IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private readonly IDiagnosticService? _diagnostics;
    private WorkerSession? _session;

    public TripoSrService(IWorkerHost _, IDiagnosticService? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }

    public TripoSrRuntimeStatus Probe() => TripoSrRuntime.Probe();

    public async Task<string> GenerateGlbAsync(
        string imagePath,
        string destinationGlb,
        CancellationToken cancellationToken = default)
    {
        return await PipelineTrace.RunAsync(_diagnostics, "AI", "ImageTo3D.TripoSR", async () =>
        {
            var status = TripoSrRuntime.Probe();
            if (status.Availability is FeatureAvailability.NotInstalled
                or FeatureAvailability.UnsupportedHardware
                or FeatureAvailability.Disabled)
                throw new InvalidOperationException(status.Message);
            if (!File.Exists(imagePath))
                throw new InvalidOperationException("ImageNotFound – input image missing.");

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationGlb))!);
            var payload = JsonSerializer.Serialize(new
            {
                imagePath = Path.GetFullPath(imagePath),
                glbPath = Path.GetFullPath(destinationGlb),
                modelDir = status.ModelDir,
                device = "auto",
                removeBackground = true,
                mcResolution = 96,
                vertexColors = true
            });

            await SendAsync("image.to3d.generate", payload, cancellationToken);
            if (!File.Exists(destinationGlb) || new FileInfo(destinationGlb).Length < 64)
                throw new InvalidOperationException("TripoSR worker did not write a valid GLB. No mesh will be faked.");
            return destinationGlb;
        }, provider: "triposr").ConfigureAwait(false);
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
        var status = TripoSrRuntime.Probe();
        if (status.Availability is FeatureAvailability.NotInstalled
            or FeatureAvailability.UnsupportedHardware
            or FeatureAvailability.Disabled)
            throw new InvalidOperationException(status.Message);
        if (status.UvPath is null || status.WorkerScript is null || status.ProjectDir is null)
            throw new InvalidOperationException("NotInstalled – TripoSR runtime paths incomplete.");

        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            _session ??= await WorkerSession.StartAsync(
                status.UvPath,
                ["run", "--project", status.ProjectDir, "python", "-u", status.WorkerScript],
                TimeSpan.FromMinutes(3),
                cancellationToken: cancellationToken);

            var result = await _session.RequestAsync(
                new WorkerRequest(method, jsonParams, JobId: Guid.NewGuid().ToString("N"), BackendId: "triposr"),
                TimeSpan.FromHours(2),
                cancellationToken);
            if (!result.Ok)
            {
                await _session.DisposeAsync();
                _session = null;
                throw new InvalidOperationException(result.ErrorMessage ?? result.ErrorCode ?? "TripoSR worker failed.");
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
