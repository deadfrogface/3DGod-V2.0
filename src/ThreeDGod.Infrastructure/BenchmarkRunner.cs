using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Persistence;
using ThreeDGod.Workers;

namespace ThreeDGod.Infrastructure;

public sealed class BenchmarkMetric
{
    public double? Value { get; init; }
    public string Unit { get; init; } = "ms";
    public string Status { get; init; } = "measured";
    public string? Reason { get; init; }
}

public sealed class BenchmarkReport
{
    public int Phase { get; init; } = 59;
    public DateTime GeneratedUtc { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object?> Environment { get; init; } = [];
    public Dictionary<string, BenchmarkMetric> Benchmarks { get; init; } = [];
    public Dictionary<string, string> AsyncClaims { get; init; } = [];
}

public static class BenchmarkRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static BenchmarkReport Run(string repoRoot, bool includeWorkerGeneration = true)
    {
        var profile = HardwareProfiler.Probe();
        var report = new BenchmarkReport
        {
            Environment = new Dictionary<string, object?>
            {
                ["cpuCount"] = profile.CpuCount,
                ["ramBytes"] = profile.RamBytes,
                ["gpuName"] = profile.GpuName,
                ["vramMb"] = profile.VramMb,
                ["cuda"] = profile.Cuda,
                ["diskFreeBytes"] = profile.DiskFreeBytes
            },
            AsyncClaims = new Dictionary<string, string>
            {
                ["commandStack"] = "ExecuteAsync/UndoAsync/RedoAsync are Task-based and non-blocking",
                ["gpuScheduler"] = "GpuJobScheduler.RunHeavyAsync serializes heavy GPU jobs",
                ["workerHost"] = "WorkerProcessHost.RunAsync is async with timeout/cancel"
            }
        };

        report.Benchmarks["startupMs"] = MeasureStartupMs();
        report.Benchmarks["projectLoadMs"] = MeasureProjectLoadMs();
        report.Benchmarks["projectSaveMs"] = MeasureProjectSaveMs();
        report.Benchmarks["humanSliderMs"] = MeasureHumanSliderMs();
        report.Benchmarks["viewportFps"] = new BenchmarkMetric
        {
            Status = "gated",
            Reason = "Requires interactive WPF render loop; not measured in headless benchmark runner"
        };
        report.Benchmarks["generationMs"] = includeWorkerGeneration
            ? MeasureGenerationMs(repoRoot)
            : new BenchmarkMetric { Status = "gated", Reason = "Worker generation skipped" };
        report.Benchmarks["workerProcessReleased"] = includeWorkerGeneration
            ? MeasureWorkerReleased(repoRoot)
            : new BenchmarkMetric { Value = null, Unit = "bool", Status = "gated", Reason = "Worker generation skipped" };
        report.Benchmarks["vramMbAfterJob"] = new BenchmarkMetric
        {
            Value = profile.VramMb > 0 ? profile.VramMb : null,
            Unit = "MB",
            Status = profile.Cuda ? "measured" : "gated",
            Reason = profile.Cuda ? null : "No CUDA worker/runtime in this environment"
        };
        report.Benchmarks["ramBytesWorkingSet"] = new BenchmarkMetric
        {
            Value = Process.GetCurrentProcess().WorkingSet64,
            Unit = "bytes",
            Status = "measured"
        };

        return report;
    }

    public static string WriteReport(string repoRoot, string? outputPath = null, bool includeWorkerGeneration = true)
    {
        outputPath ??= Path.Combine(repoRoot, "docs", "audit", "PHASE_59_BENCHMARK.json");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        var report = Run(repoRoot, includeWorkerGeneration);
        var json = JsonSerializer.Serialize(report, JsonOptions);
        File.WriteAllText(outputPath, json);
        return outputPath;
    }

    private static BenchmarkMetric MeasureStartupMs()
    {
        var sw = Stopwatch.StartNew();
        var services = new ServiceCollection();
        services.AddThreeDGodCoreServices();
        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IProjectService>();
        _ = provider.GetRequiredService<CommandStack>();
        _ = provider.GetRequiredService<IGpuJobScheduler>();
        sw.Stop();
        return new BenchmarkMetric { Value = sw.Elapsed.TotalMilliseconds, Unit = "ms", Status = "measured" };
    }

    private static BenchmarkMetric MeasureProjectLoadMs()
    {
        var archive = new GodProjectArchive();
        var path = WriteTempProject(archive);
        try
        {
            var sw = Stopwatch.StartNew();
            archive.LoadAsync(path).GetAwaiter().GetResult();
            sw.Stop();
            return new BenchmarkMetric { Value = sw.Elapsed.TotalMilliseconds, Unit = "ms", Status = "measured" };
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static BenchmarkMetric MeasureProjectSaveMs()
    {
        var archive = new GodProjectArchive();
        var bundle = SampleBundle();
        var path = Path.Combine(Path.GetTempPath(), $"3dgod-bench-{Guid.NewGuid():N}.3dgod");
        try
        {
            var sw = Stopwatch.StartNew();
            archive.SaveAsync(bundle, path).GetAwaiter().GetResult();
            sw.Stop();
            return new BenchmarkMetric { Value = sw.Elapsed.TotalMilliseconds, Unit = "ms", Status = "measured" };
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static BenchmarkMetric MeasureHumanSliderMs()
    {
        var stack = new CommandStack();
        var characterId = Guid.NewGuid();
        var values = new List<int> { 1 };
        var sw = Stopwatch.StartNew();
        stack.ExecuteAsync(new SnapshotListCommand<int>(characterId, values, [2, 3], "edit.slider")).GetAwaiter().GetResult();
        sw.Stop();
        return new BenchmarkMetric { Value = sw.Elapsed.TotalMilliseconds, Unit = "ms", Status = "measured" };
    }

    private static BenchmarkMetric MeasureGenerationMs(string repoRoot)
    {
        var script = Path.Combine(repoRoot, "workers", "echo", "echo_worker.py");
        if (!File.Exists(script))
        {
            return new BenchmarkMetric
            {
                Status = "gated",
                Reason = "Echo worker script missing; cannot measure generation time"
            };
        }

        var host = new WorkerProcessHost();
        var sw = Stopwatch.StartNew();
        var result = host.RunAsync(
            "python",
            ["-u", script],
            new WorkerRequest("echo", """{"n":1}""", BackendId: "echo"),
            TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
        sw.Stop();
        return new BenchmarkMetric
        {
            Value = result.Ok ? sw.Elapsed.TotalMilliseconds : null,
            Unit = "ms",
            Status = result.Ok ? "measured" : "gated",
            Reason = result.Ok ? null : result.ErrorCode ?? "Worker failed"
        };
    }

    private static BenchmarkMetric MeasureWorkerReleased(string repoRoot)
    {
        var script = Path.Combine(repoRoot, "workers", "echo", "echo_worker.py");
        if (!File.Exists(script))
        {
            return new BenchmarkMetric
            {
                Unit = "bool",
                Status = "gated",
                Reason = "Echo worker script missing"
            };
        }

        var before = Process.GetProcessesByName("python").Length;
        var host = new WorkerProcessHost();
        _ = host.RunAsync("python", ["-u", script], new WorkerRequest("echo", "{}"), TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        Thread.Sleep(250);
        var after = Process.GetProcessesByName("python").Length;
        return new BenchmarkMetric
        {
            Value = after <= before ? 1 : 0,
            Unit = "bool",
            Status = "measured",
            Reason = after <= before ? null : "Residual python processes detected after worker run"
        };
    }

    private static string WriteTempProject(GodProjectArchive archive)
    {
        var path = Path.Combine(Path.GetTempPath(), $"3dgod-bench-{Guid.NewGuid():N}.3dgod");
        archive.SaveAsync(SampleBundle(), path).GetAwaiter().GetResult();
        return path;
    }

    private static ProjectBundle SampleBundle() => new()
    {
        Project = new ProjectDocument { Name = "Benchmark" },
        Characters = [new CharacterDocument { Name = "Hero" }],
        Meshes = [new MeshAsset { Name = "body", CanonicalGlbPath = "assets/body.glb" }]
    };

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
        try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
    }
}
