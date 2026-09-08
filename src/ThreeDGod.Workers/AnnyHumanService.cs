using System.Text.Json;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Workers;

public sealed class AnnyRuntimeStatus
{
    public FeatureAvailability Availability { get; init; } = FeatureAvailability.NotInstalled;
    public string Message { get; init; } = "";
    public string? UvPath { get; init; }
    public string? WorkerScript { get; init; }
    public string? ProjectDir { get; init; }
}

public static class AnnyRuntime
{
    public static AnnyRuntimeStatus Probe(string? repoRoot = null)
    {
        var root = repoRoot ?? FindRepoRoot();
        var projectDir = Path.Combine(root, "workers", "anny");
        var script = Path.Combine(projectDir, "anny_worker.py");
        var lockFile = Path.Combine(projectDir, "uv.lock");
        var uv = FindUv();

        if (!File.Exists(script))
            return new AnnyRuntimeStatus { Availability = FeatureAvailability.NotInstalled, Message = "NotInstalled – Anny worker script missing." };
        if (uv is null)
            return new AnnyRuntimeStatus { Availability = FeatureAvailability.NotInstalled, Message = "NotInstalled – uv is not on PATH." };
        if (!File.Exists(lockFile) && !Directory.Exists(Path.Combine(projectDir, ".venv")))
            return new AnnyRuntimeStatus
            {
                Availability = FeatureAvailability.NotInstalled,
                Message = "NotInstalled – Anny uv environment is not installed. No mesh will be generated.",
                UvPath = uv,
                WorkerScript = script,
                ProjectDir = projectDir
            };

        return new AnnyRuntimeStatus
        {
            Availability = FeatureAvailability.Experimental,
            Message = "Experimental – isolated Anny worker via uv. Apache/CC0 paths only.",
            UvPath = uv,
            WorkerScript = script,
            ProjectDir = projectDir
        };
    }

    public static string? FindUv()
    {
        var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin", "uv.exe");
        if (File.Exists(local))
            return local;
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(dir, "uv.exe");
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "3DGodCreator.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }
}

public sealed class AnnyCatalog
{
    public string BackendId { get; init; } = "anny";
    public string BackendVersion { get; init; } = "";
    public IReadOnlyList<string> PhenotypeKeys { get; init; } = [];
    public IReadOnlyList<string> LocalChangeKeys { get; init; } = [];
    public IReadOnlyList<string> FacialActionKeys { get; init; } = [];
    public int Count => PhenotypeKeys.Count + LocalChangeKeys.Count + FacialActionKeys.Count;
}

public sealed class AnnyGenerateRequest
{
    public IReadOnlyDictionary<string, float> Phenotypes { get; init; } = new Dictionary<string, float>();
    public IReadOnlyDictionary<string, float> LocalChanges { get; init; } = new Dictionary<string, float>();
    public IReadOnlyDictionary<string, float> FacialActions { get; init; } = new Dictionary<string, float>();
}

public sealed class AnnyHumanService : IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private WorkerSession? _session;

    public AnnyHumanService(IWorkerHost _)
    {
    }

    public AnnyRuntimeStatus Probe() => AnnyRuntime.Probe();

    public async Task<AnnyCatalog> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendAsync("human.catalog", "{}", cancellationToken);
        using var doc = JsonDocument.Parse(result.JsonPayload ?? "{}");
        if (!doc.RootElement.TryGetProperty("data", out var data))
            throw new InvalidOperationException("Anny catalog returned no data.");
        return new AnnyCatalog
        {
            BackendId = data.TryGetProperty("backendId", out var id) ? id.GetString() ?? "anny" : "anny",
            BackendVersion = data.TryGetProperty("backendVersion", out var ver) ? ver.GetString() ?? "" : "",
            PhenotypeKeys = ReadStringArray(data, "phenotypeKeys"),
            LocalChangeKeys = ReadStringArray(data, "localChangeKeys"),
            FacialActionKeys = ReadStringArray(data, "facialActionKeys")
        };
    }

    public Task<string> GenerateGlbAsync(string destinationGlb, IReadOnlyDictionary<string, float>? phenotypes = null, CancellationToken cancellationToken = default) =>
        GenerateGlbAsync(destinationGlb, new AnnyGenerateRequest { Phenotypes = phenotypes ?? new Dictionary<string, float>() }, cancellationToken);

    public async Task<string> GenerateGlbAsync(string destinationGlb, AnnyGenerateRequest request, CancellationToken cancellationToken = default)
    {
        var status = AnnyRuntime.Probe();
        if (status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled)
            throw new InvalidOperationException(status.Message);

        var objPath = Path.ChangeExtension(destinationGlb, ".obj");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationGlb))!);
        var payload = JsonSerializer.Serialize(new
        {
            objPath,
            phenotypes = request.Phenotypes,
            localChanges = request.LocalChanges,
            facialActions = request.FacialActions
        });

        var result = await SendAsync("human.generate", payload, cancellationToken);
        if (!File.Exists(objPath))
            throw new InvalidOperationException("Anny worker did not write an OBJ. No mesh will be faked.");

        Mesh.TriangleMeshExport.ObjToGlb(objPath, destinationGlb);
        if (!File.Exists(destinationGlb) || new FileInfo(destinationGlb).Length < 64)
            throw new InvalidOperationException("GLB export from Anny OBJ failed.");
        return destinationGlb;
    }

    public static AnnyGenerateRequest FromState(ParametricHumanState state) =>
        new()
        {
            Phenotypes = new Dictionary<string, float>(state.PhenotypeParameters),
            LocalChanges = new Dictionary<string, float>(state.LocalShapeParameters),
            FacialActions = new Dictionary<string, float>(state.FacialActionParameters)
        };

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (_session is not null)
            await _session.DisposeAsync();
        _sessionLock.Dispose();
    }

    private async Task<WorkerRunResult> SendAsync(string method, string jsonParams, CancellationToken cancellationToken)
    {
        var status = AnnyRuntime.Probe();
        if (status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled)
            throw new InvalidOperationException(status.Message);
        if (status.UvPath is null || status.WorkerScript is null || status.ProjectDir is null)
            throw new InvalidOperationException("NotInstalled – Anny runtime paths incomplete.");

        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            _session ??= await WorkerSession.StartAsync(
                status.UvPath,
                ["run", "--project", status.ProjectDir, "python", "-u", status.WorkerScript],
                TimeSpan.FromMinutes(2),
                cancellationToken: cancellationToken);

            var result = await _session.RequestAsync(
                new WorkerRequest(method, jsonParams, JobId: Guid.NewGuid().ToString("N"), BackendId: "anny"),
                TimeSpan.FromMinutes(15),
                cancellationToken);
            if (!result.Ok)
            {
                await _session.DisposeAsync();
                _session = null;
                throw new InvalidOperationException(result.ErrorMessage ?? result.ErrorCode ?? "Anny worker failed.");
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

    private static IReadOnlyList<string> ReadStringArray(JsonElement data, string name)
    {
        if (!data.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];
        return arr.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToArray();
    }
}
