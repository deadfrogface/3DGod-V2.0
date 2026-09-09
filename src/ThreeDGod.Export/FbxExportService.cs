using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;

namespace ThreeDGod.Export;

public sealed class FbxExportService : IFbxExportService
{
    private readonly Func<string, string, string?> _runBlenderExport;
    private readonly Func<bool> _isBlenderAvailable;
    private readonly IDiagnosticService? _diagnostics;

    public FbxExportService(
        Func<bool> isBlenderAvailable,
        Func<string, string, string?> runBlenderExport,
        IDiagnosticService? diagnostics = null)
    {
        _isBlenderAvailable = isBlenderAvailable;
        _runBlenderExport = runBlenderExport;
        _diagnostics = diagnostics;
    }

    public string Export(string sourceGlb, string destinationFbx, string? assetName = null, bool runUe5Preflight = true)
    {
        if (!_isBlenderAvailable())
            throw new InvalidOperationException("GATED_NOT_INSTALLED – Blender runtime missing; FBX export not executed.");

        if (string.IsNullOrWhiteSpace(sourceGlb) || !File.Exists(sourceGlb))
            throw new FileNotFoundException("GLB source missing.", sourceGlb);

        if (runUe5Preflight)
        {
            PipelineTrace.Run(_diagnostics, "Export", "Export.Preflight", () =>
            {
                var report = UnrealEngine5ExportProfile.EvaluateGlb(
                    sourceGlb,
                    assetName ?? Path.GetFileNameWithoutExtension(destinationFbx),
                    requireSkin: false,
                    _diagnostics);
                if (!report.Passed)
                    throw new InvalidOperationException(
                        "UE5 GLB preflight hard-fail: " + string.Join("; ", report.HardMessages));
            }, provider: "ue5-skeletal-mesh");
        }

        PipelineTrace.Run(_diagnostics, "Export", "Export.Write", () =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationFbx))!);
            var error = _runBlenderExport(sourceGlb, destinationFbx);
            if (!string.IsNullOrWhiteSpace(error))
                throw new InvalidOperationException(error);
        }, provider: "blender-fbx");

        var sanity = PipelineTrace.Run(_diagnostics, "Export", "Export.Preflight", () =>
            FbxSanity.Check(destinationFbx), provider: "ue5-preflight");

        if (!sanity.Passed)
            throw new InvalidOperationException(string.Join("; ", sanity.Issues));

        return destinationFbx;
    }
}
