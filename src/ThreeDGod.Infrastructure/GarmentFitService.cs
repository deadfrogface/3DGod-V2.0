using System.Text.Json;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Domain;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Workers;

namespace ThreeDGod.Infrastructure;

public sealed class GarmentFitService : IGarmentFitService
{
    private readonly AnnyHumanService _anny;
    private readonly GarmentCodeService _garment;
    private readonly IDiagnosticService? _diagnostics;

    public GarmentFitService(AnnyHumanService anny, GarmentCodeService garment, IDiagnosticService? diagnostics = null)
    {
        _anny = anny;
        _garment = garment;
        _diagnostics = diagnostics;
    }

    public async Task<GarmentFitResult> FitJacketToPresetAsync(string presetName, string workRoot, CancellationToken cancellationToken = default)
    {
        return await PipelineTrace.RunAsync(_diagnostics, "Garment", "Garment.Fit", async () =>
        {
            var anny = AnnyRuntime.Probe();
            if (anny.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled)
                throw new InvalidOperationException(anny.Message);

            Directory.CreateDirectory(workRoot);
            var store = new AnnyPresetStore(AnnyRuntime.FindRepoRoot());
            var preset = store.Load(presetName);
            var bodyGlb = Path.Combine(workRoot, presetName + "-body.glb");
            await _anny.GenerateGlbAsync(bodyGlb, AnnyHumanService.FromState(AnnyPresetMapper.ToState(preset)), cancellationToken);
            return await FitJacketToBodyAsync(bodyGlb, workRoot, presetName, cancellationToken);
        }, provider: "geometry3Sharp").ConfigureAwait(false);
    }

    public async Task<GarmentFitResult> FitJacketToBodyAsync(string bodyGlb, string workRoot, string presetName, CancellationToken cancellationToken = default)
    {
        return await PipelineTrace.RunAsync(_diagnostics, "Garment", "Garment.Fit", async () =>
        {
            var gc = GarmentCodeRuntime.Probe();
            if (gc.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled)
                throw new InvalidOperationException(gc.Message);
            if (!File.Exists(bodyGlb))
                throw new InvalidOperationException("Body GLB is missing. No fit will be faked.");

            Directory.CreateDirectory(workRoot);
            var (bodyPos, bodyIdx) = MeshCompare.ReadMesh(bodyGlb);
            var measurements = GarmentProximityFitter.Measure(bodyPos, bodyGlb);
            var jacketReq = JacketParamsFromBody(measurements);
            var jacketGlb = Path.Combine(workRoot, presetName + "-jacket.glb");
            await _garment.GenerateJacketGlbAsync(jacketGlb, jacketReq, cancellationToken);
            var (garmentPos, garmentIdx) = MeshCompare.ReadMesh(jacketGlb);
            var (fitted, report) = GarmentProximityFitter.Fit(bodyPos, bodyIdx, garmentPos, garmentIdx, measurements);
            var fittedGlb = Path.Combine(workRoot, presetName + "-jacket-fitted.glb");
            TriangleMeshExport.WriteGlb(fittedGlb, fitted, garmentIdx);
            report.PresetName = presetName;
            report.BodyGlb = bodyGlb;
            report.GarmentGlb = jacketGlb;
            report.FittedGlb = fittedGlb;
            var reportPath = Path.Combine(workRoot, presetName + "-clipping.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return new GarmentFitResult { FittedGlb = fittedGlb, ReportPath = reportPath, Report = report };
        }, provider: "geometry3Sharp").ConfigureAwait(false);
    }
    public static GarmentCodeJacketRequest JacketParamsFromBody(BodyMeasurements m)
    {
        var heightCm = m.HeightM * 100f;
        var chestCm = MathF.Max(28f, m.ChestWidthM * 100f);
        return new GarmentCodeJacketRequest
        {
            WidthCm = chestCm * 1.08f,
            LengthCm = Math.Clamp(heightCm * 0.35f, 40f, 90f),
            SleeveLengthCm = Math.Clamp(heightCm * 0.28f, 25f, 70f)
        };
    }
}
