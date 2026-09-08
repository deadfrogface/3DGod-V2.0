using ThreeDGod.Core.Diagnostics;

namespace ThreeDGodCreator.Core.Tests;

public class CursorReportBuilderTests
{
    [Fact]
    public void CreateCursorReport_ContainsRequiredFields_AndRedactsSecrets()
    {
        Exception boom;
        try
        {
            ThrowForReport();
            throw new InvalidOperationException("no throw");
        }
        catch (Exception ex)
        {
            boom = ex;
        }

        var issue = new CSharpExceptionEnricher().Enrich(
            boom,
            "UseCase.Boundary",
            [
                new DiagnosticBreadcrumb { Pipeline = "Test.Pipeline", Stage = "Parse", Status = "Completed" },
                new DiagnosticBreadcrumb { Pipeline = "Test.Pipeline", Stage = "Validate", Status = "Failed", Provider = "echo" }
            ],
            correlationId: "corr-x",
            jobId: "job-1",
            backendId: "echo");

        var report = CursorReportBuilder.CreateCursorReport(
            issue,
            stdoutTail: "ok",
            stderrTail: "Authorization: Bearer super-secret-token",
            versions: "net10",
            hardware: "cpu",
            expectedBehavior: "probe throws and is captured");

        Assert.Contains(CursorReportBuilder.HeaderInstruction, report, StringComparison.Ordinal);
        Assert.Contains("Error Code:", report, StringComparison.Ordinal);
        Assert.Contains("Exception:", report, StringComparison.Ordinal);
        Assert.Contains("Pipeline: Test.Pipeline", report, StringComparison.Ordinal);
        Assert.Contains("Last Successful Stage: Parse", report, StringComparison.Ordinal);
        Assert.Contains("Failing Stage: Validate", report, StringComparison.Ordinal);
        Assert.Contains("Backend/Worker: echo", report, StringComparison.Ordinal);
        Assert.Contains("Source File:", report, StringComparison.Ordinal);
        Assert.Contains("Method: ThrowForReport", report, StringComparison.Ordinal);
        Assert.Contains("Line:", report, StringComparison.Ordinal);
        Assert.Contains("Failing Code:", report, StringComparison.Ordinal);
        Assert.Contains("Code Context:", report, StringComparison.Ordinal);
        Assert.Contains("Relevant Stack Trace:", report, StringComparison.Ordinal);
        Assert.Contains("stdout:", report, StringComparison.Ordinal);
        Assert.Contains("stderr:", report, StringComparison.Ordinal);
        Assert.Contains("Versions:", report, StringComparison.Ordinal);
        Assert.Contains("Hardware:", report, StringComparison.Ordinal);
        Assert.Contains("Expected Behavior:", report, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-token", report, StringComparison.Ordinal);
        Assert.Contains("Bearer ***", report, StringComparison.Ordinal);
        Assert.True(issue.Location.Line > 0);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void ThrowForReport() => throw new InvalidOperationException("report-probe");
}
