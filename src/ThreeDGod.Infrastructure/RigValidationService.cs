using ThreeDGod.Application;
using ThreeDGod.Rigging;

namespace ThreeDGod.Infrastructure;

public sealed class RigValidationService : IRigValidator
{
    public bool ValidateGlb(string glbPath, out IReadOnlyList<string> failures, bool requireHumanoid = true)
    {
        var report = RigValidator.ValidateGlb(glbPath, requireHumanoid);
        failures = report.Failures.Select(f => f.Code + ": " + f.Message).ToList();
        return report.Passed;
    }
}
