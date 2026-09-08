using ThreeDGod.Infrastructure.Logging;

namespace ThreeDGodCreator.Core.Tests;

[Collection("GodLog")]
public class GodLogTests
{
    [Fact]
    public void WorkerException_IsLoggedAndRethrown_NotSwallowed()
    {
        var dir = Path.Combine(Path.GetTempPath(), "3dgod-logs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            GodLog.Initialize(dir);
            var inner = new InvalidOperationException("echo-worker exploded");
            var thrown = Assert.Throws<WorkerLoggedException>(() =>
                GodLog.ThrowWorkerException(inner, correlationId: "corr-1", jobId: "job-9", backendId: "echo"));
            Assert.Contains("echo-worker exploded", thrown.Message, StringComparison.Ordinal);
            Assert.Same(inner, thrown.InnerException);
            Assert.Equal("corr-1", thrown.CorrelationId);
            GodLog.Close();

            var files = Directory.GetFiles(dir, "*.log");
            Assert.NotEmpty(files);
            var text = string.Join("\n", files.Select(File.ReadAllText));
            Assert.Contains("echo-worker exploded", text, StringComparison.Ordinal);
            Assert.Contains("corr-1", text, StringComparison.Ordinal);
            Assert.Contains("job-9", text, StringComparison.Ordinal);
            Assert.Contains("echo", text, StringComparison.Ordinal);
        }
        finally
        {
            GodLog.Close();
            try { Directory.Delete(dir, true); } catch { /* temp */ }
        }
    }

    [Fact]
    public void Write_IncludesCorrelationJobAndBackend()
    {
        var dir = Path.Combine(Path.GetTempPath(), "3dgod-logs-" + Guid.NewGuid().ToString("N"));
        try
        {
            GodLog.Initialize(dir);
            GodLog.Write("hello structured", "c", "j", "b", durationMs: 12, result: "ok", errorCode: "");
            GodLog.Close();
            var text = string.Join("\n", Directory.GetFiles(dir, "*.log").Select(File.ReadAllText));
            Assert.Contains("hello structured", text, StringComparison.Ordinal);
            Assert.Contains("corr=c", text, StringComparison.Ordinal);
            Assert.Contains("job=j", text, StringComparison.Ordinal);
            Assert.Contains("backend=b", text, StringComparison.Ordinal);
            Assert.Contains("durationMs=12", text, StringComparison.Ordinal);
            Assert.Contains("app=", text, StringComparison.Ordinal);
        }
        finally
        {
            GodLog.Close();
            try { Directory.Delete(dir, true); } catch { /* temp */ }
        }
    }
}
