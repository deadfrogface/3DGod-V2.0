using ThreeDGod.Core.Diagnostics;

namespace ThreeDGodCreator.Core.Tests;

public class PipelineBreadcrumbTests
{
    [Fact]
    public void SimulatedPipelineFailure_ExposesLastSuccessAndFailingStage()
    {
        var svc = new DiagnosticService();
        svc.AddBreadcrumb(new DiagnosticBreadcrumb { Pipeline = "Import", Stage = "Import.Parse", Status = "Completed" });
        svc.AddBreadcrumb(new DiagnosticBreadcrumb { Pipeline = "Import", Stage = "Import.Validate", Status = "Failed", Provider = "assimp" });
        var issue = svc.Capture(new InvalidOperationException("bad mesh"), "Import.Validate");
        Assert.Equal("Import", issue.Pipeline);
        Assert.Equal("Import.Parse", issue.LastSuccessfulStage);
        Assert.Equal("Import.Validate", issue.FailingStage);
    }
}
