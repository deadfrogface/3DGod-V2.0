using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Rigging;

namespace ThreeDGod.Infrastructure;

public sealed class RigValidationService : IRigValidator
{
    private readonly IDiagnosticService? _diagnostics;

    public RigValidationService(IDiagnosticService? diagnostics = null) => _diagnostics = diagnostics;

    public bool ValidateGlb(string glbPath, out IReadOnlyList<string> failures, bool requireHumanoid = true)
    {
        PipelineTrace.Stage(_diagnostics, "Rigging", "Rig.Skeleton", "Started", "rig-validator");
        var report = RigValidator.ValidateGlb(glbPath, requireHumanoid);
        failures = report.Failures.Select(f => f.Code + ": " + f.Message).ToList();
        PipelineTrace.Stage(
            _diagnostics,
            "Rigging",
            "Rig.Skeleton",
            report.Passed ? "Completed" : "Failed",
            "rig-validator");
        return report.Passed;
    }
}
