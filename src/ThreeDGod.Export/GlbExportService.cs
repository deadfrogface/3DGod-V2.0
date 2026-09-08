using SharpGLTF.Schema2;
using ThreeDGod.Mesh;

namespace ThreeDGod.Export;

public static class GlbExportService
{
    public static string Export(string sourceGlb, string destinationGlb)
    {
        var before = CanonicalGltfPipeline.Load(sourceGlb);
        File.Copy(sourceGlb, destinationGlb, overwrite: true);
        var after = CanonicalGltfPipeline.Load(destinationGlb);
        if (after.VertexCount != before.VertexCount)
            throw new InvalidOperationException("GLB export changed vertex count.");
        return destinationGlb;
    }
}
