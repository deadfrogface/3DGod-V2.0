using SharpGLTF.Schema2;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Mesh;

namespace ThreeDGod.Export;

public static class GlbExportService
{
    public static string Export(string sourceGlb, string destinationGlb, IDiagnosticService? diagnostics = null)
    {
        PipelineTrace.Run(diagnostics, "Export", "Export.Preflight", () =>
        {
            if (!File.Exists(sourceGlb))
                throw new FileNotFoundException("GLB source missing.", sourceGlb);
            _ = CanonicalGltfPipeline.Load(sourceGlb);
        });

        return PipelineTrace.Run(diagnostics, "Export", "Export.Write", () =>
        {
            var before = CanonicalGltfPipeline.Load(sourceGlb);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationGlb))!);
            File.Copy(sourceGlb, destinationGlb, overwrite: true);
            var after = CanonicalGltfPipeline.Load(destinationGlb);
            if (after.VertexCount != before.VertexCount)
                throw new InvalidOperationException("GLB export changed vertex count.");
            return destinationGlb;
        }, provider: "sharpgltf-copy");
    }
}
