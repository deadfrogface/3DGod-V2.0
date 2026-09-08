using System.Diagnostics;
using System.Text.Json;
using ThreeDGod.Application;

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

public sealed class AnnyHumanService
{
    private readonly IWorkerHost _host;

    public AnnyHumanService(IWorkerHost host) => _host = host;

    public AnnyRuntimeStatus Probe() => AnnyRuntime.Probe();

    public async Task<string> GenerateGlbAsync(string destinationGlb, IReadOnlyDictionary<string, float>? phenotypes = null, CancellationToken cancellationToken = default)
    {
        var status = AnnyRuntime.Probe();
        if (status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled)
            throw new InvalidOperationException(status.Message);
        if (status.UvPath is null || status.WorkerScript is null || status.ProjectDir is null)
            throw new InvalidOperationException("NotInstalled – Anny runtime paths incomplete.");

        var objPath = Path.ChangeExtension(destinationGlb, ".obj");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationGlb))!);
        var payload = JsonSerializer.Serialize(new
        {
            objPath,
            phenotypes = phenotypes ?? new Dictionary<string, float>()
        });

        var result = await _host.RunAsync(
            status.UvPath,
            ["run", "--project", status.ProjectDir, "python", "-u", status.WorkerScript],
            new WorkerRequest("human.generate", payload, JobId: Guid.NewGuid().ToString("N"), BackendId: "anny"),
            TimeSpan.FromMinutes(15),
            cancellationToken);

        if (!result.Ok)
            throw new InvalidOperationException(result.ErrorMessage ?? result.ErrorCode ?? "Anny generate failed.");

        if (!File.Exists(objPath))
            throw new InvalidOperationException("Anny worker did not write an OBJ. No mesh will be faked.");

        Mesh.TriangleMeshExport.ObjToGlb(objPath, destinationGlb);
        if (!File.Exists(destinationGlb) || new FileInfo(destinationGlb).Length < 64)
            throw new InvalidOperationException("GLB export from Anny OBJ failed.");
        return destinationGlb;
    }
}
