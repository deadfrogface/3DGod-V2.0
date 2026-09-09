using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

[Collection("WorkerSerial")]
public class WorkerSmartDiagnosticsTests
{
    [Fact]
    public async Task PythonThrow_ReturnsStructuredTracebackForCursorReport()
    {
        var host = new WorkerProcessHost();
        var result = await host.RunAsync(
            "python",
            ["-u", Path.Combine(RepoPaths.FindRepoRoot(), "workers", "echo", "echo_worker.py")],
            new WorkerRequest("throw", "{}", JobId: "job-throw", BackendId: "echo"),
            TimeSpan.FromSeconds(10));
        Assert.False(result.Ok);
        var diag = WorkerSmartDiagnostics.TryParse(result.JsonPayload);
        Assert.NotNull(diag);
        Assert.Equal("RuntimeError", diag!.ExceptionType);
        Assert.Contains("intentional-python-exception", diag.Traceback, StringComparison.Ordinal);
        Assert.Equal("echo.throw", diag.Stage);
        Assert.Equal("hello", diag.LastSuccessfulStage);

        var issue = new DiagnosticIssue
        {
            ErrorCode = diag.Code ?? "PythonException",
            ExceptionType = diag.ExceptionType ?? "",
            Message = result.ErrorMessage ?? "",
            BackendId = "echo",
            Pipeline = "Worker.Run",
            LastSuccessfulStage = diag.LastSuccessfulStage,
            FailingStage = diag.Stage,
            Location = new SourceLocation { SymbolStatus = SymbolStatus.Unavailable }
        };
        var report = CursorReportBuilder.CreateCursorReport(issue, stderrTail: diag.Traceback);
        Assert.Contains("intentional-python-exception", report, StringComparison.Ordinal);
        Assert.Contains("Failing Stage: echo.throw", report, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HardCrash_CapturesExitCodeAndDoesNotFakeResult()
    {
        var host = new WorkerProcessHost();
        var result = await host.RunAsync(
            "python",
            ["-u", Path.Combine(RepoPaths.FindRepoRoot(), "workers", "echo", "echo_worker.py")],
            new WorkerRequest("crash", "{}", JobId: "job-crash", BackendId: "echo"),
            TimeSpan.FromSeconds(10));
        Assert.False(result.Ok);
        Assert.True(result.Crashed || result.ExitCode == 2);
        var crash = WorkerSmartDiagnostics.FromCrash(result.ExitCode ?? -1, result.JsonPayload, "hello", "crash", "echo");
        Assert.Equal("process.exit", crash.Stage);
        Assert.NotNull(crash.ExitCode);
    }
}
