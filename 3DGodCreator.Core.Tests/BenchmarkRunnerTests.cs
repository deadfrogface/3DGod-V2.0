using System.Text.Json;
using ThreeDGod.Infrastructure;

namespace ThreeDGodCreator.Core.Tests;

public class BenchmarkRunnerTests
{
    [Fact]
    public void BenchmarkRunner_WritesJsonReport_WithMeasuredAndGatedMetrics()
    {
        var root = RepoPaths.FindRepoRoot();
        var repoOutput = Path.Combine(root, "docs", "audit", "PHASE_59_BENCHMARK.json");
        var written = BenchmarkRunner.WriteReport(root, repoOutput);
        Assert.True(File.Exists(written));
        using var doc = JsonDocument.Parse(File.ReadAllText(written));
        Assert.Equal(59, doc.RootElement.GetProperty("phase").GetInt32());
        var benchmarks = doc.RootElement.GetProperty("benchmarks");
        Assert.Equal("measured", benchmarks.GetProperty("startupMs").GetProperty("status").GetString());
        Assert.Equal("measured", benchmarks.GetProperty("projectLoadMs").GetProperty("status").GetString());
        Assert.Equal("gated", benchmarks.GetProperty("viewportFps").GetProperty("status").GetString());
        Assert.True(doc.RootElement.GetProperty("asyncClaims").GetProperty("commandStack").GetString()?.Contains("async", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BenchmarkRunner_ProjectSaveAndSlider_AreNonBlockingTasks()
    {
        var report = BenchmarkRunner.Run(RepoPaths.FindRepoRoot(), includeWorkerGeneration: false);
        Assert.Equal("measured", report.Benchmarks["humanSliderMs"].Status);
        Assert.Equal("measured", report.Benchmarks["projectSaveMs"].Status);
        Assert.Contains("async", report.AsyncClaims["commandStack"], StringComparison.OrdinalIgnoreCase);
    }
}
