using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Rigging;

namespace ThreeDGod.Infrastructure;

public sealed class GarmentSkinService : IGarmentSkinService
{
    private readonly IDiagnosticService? _diagnostics;

    public GarmentSkinService(IDiagnosticService? diagnostics = null) => _diagnostics = diagnostics;

    public string BindToBody(string skinnedBodyGlb, string garmentGlb, string destinationGlb) =>
        PipelineTrace.Run(_diagnostics, "Rigging", "Rig.WeightTransfer",
            () => GarmentSkinBinder.Bind(skinnedBodyGlb, garmentGlb, destinationGlb),
            provider: "nearest-vertex");
}
