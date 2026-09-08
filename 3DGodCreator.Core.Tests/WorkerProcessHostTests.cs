using ThreeDGod.Application;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

public class WorkerProcessHostTests
{
    private static string Python => "python";
    private static string EchoScript => Path.Combine(RepoPaths.FindRepoRoot(), "workers", "echo", "echo_worker.py");

    [Fact]
    public async Task EchoWorker_HelloRequestProgressResult_Succeeds()
    {
        var host = new WorkerProcessHost();
        var result = await host.RunAsync(
            Python,
            ["-u", EchoScript],
            new WorkerRequest("echo", """{"n":1}""", JobId: "job-echo", BackendId: "echo"),
            TimeSpan.FromSeconds(10));
        Assert.True(result.Ok, result.ErrorMessage);
        Assert.Contains("result", result.JsonPayload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Crash_DoesNotTakeDownHost()
    {
        var host = new WorkerProcessHost();
        var result = await host.RunAsync(
            Python,
            ["-u", EchoScript],
            new WorkerRequest("crash", "{}"),
            TimeSpan.FromSeconds(10));
        Assert.False(result.Ok);
        Assert.True(result.Crashed || result.ExitCode == 2 || result.ErrorCode is "Crash" or "NoHello");
    }

    [Fact]
    public async Task Hang_TimesOut_AndHostStaysStable()
    {
        var host = new WorkerProcessHost();
        var result = await host.RunAsync(
            Python,
            ["-u", EchoScript],
            new WorkerRequest("hang", "{}"),
            TimeSpan.FromMilliseconds(800));
        Assert.False(result.Ok);
        Assert.True(result.TimedOut);
        Assert.Equal("Timeout", result.ErrorCode);
    }

    [Fact]
    public async Task Cancel_StopsRequest()
    {
        var host = new WorkerProcessHost();
        using var cts = new CancellationTokenSource(400);
        var result = await host.RunAsync(
            Python,
            ["-u", EchoScript],
            new WorkerRequest("hang", "{}"),
            TimeSpan.FromSeconds(10),
            cts.Token);
        Assert.False(result.Ok);
        Assert.True(result.Cancelled || result.TimedOut);
    }

    [Fact]
    public async Task InvalidJson_IsReported_WithoutThrowingToCaller()
    {
        var host = new WorkerProcessHost();
        var script = """
import sys
sys.stdout.write('{"v":"3dgod-worker/1","type":"hello","worker":"echo"}\n')
sys.stdout.flush()
sys.stdout.write('{not-json\n')
sys.stdout.flush()
sys.stdin.readline()
""";
        var result = await host.RunAsync(Python, ["-u", "-c", script], new WorkerRequest("echo", "{}"), TimeSpan.FromSeconds(8));
        Assert.False(result.Ok);
        Assert.Equal("InvalidJson", result.ErrorCode);
    }

    [Fact]
    public async Task OversizedLine_IsRejected()
    {
        var host = new WorkerProcessHost(null, 32);
        var script = """
import sys
sys.stdout.write('{"v":"3dgod-worker/1","type":"hello","worker":"echo"}\n')
sys.stdout.flush()
sys.stdout.write('{' + ('a'*200) + '}\n')
sys.stdout.flush()
sys.stdin.readline()
""";
        var result = await host.RunAsync(Python, ["-u", "-c", script], new WorkerRequest("echo", "{}"), TimeSpan.FromSeconds(8));
        Assert.False(result.Ok);
        Assert.Equal("OversizedLine", result.ErrorCode);
    }
}
