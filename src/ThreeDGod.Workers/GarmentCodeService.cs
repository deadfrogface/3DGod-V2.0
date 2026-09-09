using System.Text.Json;
using ThreeDGod.Application;

namespace ThreeDGod.Workers;

public sealed class GarmentCodeRuntimeStatus
{
    public FeatureAvailability Availability { get; init; } = FeatureAvailability.NotInstalled;
    public string Message { get; init; } = "";
    public string? UvPath { get; init; }
    public string? WorkerScript { get; init; }
    public string? ProjectDir { get; init; }
}

public static class GarmentCodeRuntime
{
    public static GarmentCodeRuntimeStatus Probe(string? repoRoot = null)
    {
        var root = repoRoot ?? AnnyRuntime.FindRepoRoot();
        var projectDir = Path.Combine(root, "workers", "garmentcode");
        var script = Path.Combine(projectDir, "garmentcode_worker.py");
        var lockFile = Path.Combine(projectDir, "uv.lock");
        var uv = AnnyRuntime.FindUv();

        if (!File.Exists(script))
            return new GarmentCodeRuntimeStatus { Availability = FeatureAvailability.NotInstalled, Message = "NotInstalled – GarmentCode worker script missing." };
        if (uv is null)
            return new GarmentCodeRuntimeStatus { Availability = FeatureAvailability.NotInstalled, Message = "NotInstalled – uv is not on PATH." };
        if (!File.Exists(lockFile) && !Directory.Exists(Path.Combine(projectDir, ".venv")))
            return new GarmentCodeRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message = "NotInstalled – GarmentCode uv environment is not installed. No jacket will be generated.",
                UvPath = uv,
                WorkerScript = script,
                ProjectDir = projectDir
            };

        return new GarmentCodeRuntimeStatus
        {
            Availability = FeatureAvailability.Experimental,
            Message = "Experimental – isolated pygarment pattern worker via uv. MIT core. Warp/CGAL sim not bundled.",
            UvPath = uv,
            WorkerScript = script,
            ProjectDir = projectDir
        };
    }
}

public sealed class GarmentCodeJacketRequest
{
    public float SleeveLengthCm { get; init; } = 45f;
    public float LengthCm { get; init; } = 60f;
    public float WidthCm { get; init; } = 40f;
}

public sealed class GarmentCodeService : IGarmentCodeService, IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private WorkerSession? _session;

    public GarmentCodeService(IWorkerHost _)
    {
    }

    public FeatureAvailability Probe() => GarmentCodeRuntime.Probe().Availability;

    public string ProbeMessage() => GarmentCodeRuntime.Probe().Message;

    public Task<string> GenerateJacketGlbAsync(string destinationGlb, CancellationToken cancellationToken = default) =>
        GenerateJacketGlbAsync(destinationGlb, new GarmentCodeJacketRequest(), cancellationToken);

    public async Task<string> GenerateJacketGlbAsync(
        string destinationGlb,
        GarmentCodeJacketRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = GarmentCodeRuntime.Probe();
        if (status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled)
            throw new InvalidOperationException(status.Message);

        var objPath = Path.ChangeExtension(destinationGlb, ".obj");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationGlb))!);
        var payload = JsonSerializer.Serialize(new
        {
            objPath,
            sleeveLengthCm = request.SleeveLengthCm,
            lengthCm = request.LengthCm,
            widthCm = request.WidthCm
        });

        var result = await SendAsync("garment.jacket", payload, cancellationToken);
        if (!File.Exists(objPath))
            throw new InvalidOperationException("GarmentCode worker did not write an OBJ. No mesh will be faked.");

        using var doc = JsonDocument.Parse(result.JsonPayload ?? "{}");
        if (!doc.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("panelCount", out var panels) ||
            panels.GetInt32() < 4)
            throw new InvalidOperationException("GarmentCode jacket must contain at least 4 pygarment panels.");

        var patternPath = data.TryGetProperty("patternPath", out var pp) ? pp.GetString() : Path.ChangeExtension(objPath, ".json");
        if (string.IsNullOrWhiteSpace(patternPath) || !File.Exists(patternPath))
            throw new InvalidOperationException("GarmentCode worker did not write a pattern JSON.");

        Mesh.TriangleMeshExport.ObjToGlb(objPath, destinationGlb);
        if (!File.Exists(destinationGlb) || new FileInfo(destinationGlb).Length < 64)
            throw new InvalidOperationException("GLB export from GarmentCode OBJ failed.");
        return destinationGlb;
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
        var status = GarmentCodeRuntime.Probe();
        if (status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled)
            throw new InvalidOperationException(status.Message);
        if (status.UvPath is null || status.WorkerScript is null || status.ProjectDir is null)
            throw new InvalidOperationException("NotInstalled – GarmentCode runtime paths incomplete.");

        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            _session ??= await WorkerSession.StartAsync(
                status.UvPath,
                ["run", "--project", status.ProjectDir, "python", "-u", status.WorkerScript],
                TimeSpan.FromMinutes(2),
                cancellationToken: cancellationToken);

            var result = await _session.RequestAsync(
                new WorkerRequest(method, jsonParams, JobId: Guid.NewGuid().ToString("N"), BackendId: "garmentcode"),
                TimeSpan.FromMinutes(2),
                cancellationToken);
            if (!result.Ok)
            {
                await _session.DisposeAsync();
                _session = null;
                throw new InvalidOperationException(result.ErrorMessage ?? result.ErrorCode ?? "GarmentCode worker failed.");
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
