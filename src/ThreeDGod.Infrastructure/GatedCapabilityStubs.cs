using ThreeDGod.Application;

namespace ThreeDGod.Infrastructure;

public sealed class NotInstalledImportService : IImportService
{
    public FeatureAvailability Probe() => FeatureAvailability.NotInstalled;
    public string ProbeMessage() => "Assimp/FBX import backend is not installed.";
}

public sealed class PreferSpecificExportService : IExportService
{
    public FeatureAvailability Probe() => FeatureAvailability.Available;
    public string ProbeMessage() => "Use IGlbExportService / IFbxExportService for concrete exporters.";
}

public sealed class NotInstalledAutoRigBackend : IAutoRigBackend, IRiggingService
{
    public string BackendId => "autoroot-not-installed";
    public FeatureAvailability Probe() => FeatureAvailability.NotInstalled;
    public string ProbeMessage() =>
        "Auto-rig backend is not installed. Freeform distance weights and authored humanoid test rigs remain available.";
}
