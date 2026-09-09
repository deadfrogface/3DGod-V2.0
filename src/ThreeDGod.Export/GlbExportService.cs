using System.Numerics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Mesh;

namespace ThreeDGod.Export;

public sealed class GlbExportService : IGlbExportService
{
    private readonly IDiagnosticService? _diagnostics;

    public GlbExportService(IDiagnosticService? diagnostics = null) => _diagnostics = diagnostics;

    public string WriteCompleteScene(string destinationGlb) => WriteCompleteSceneStatic(destinationGlb, _diagnostics);

    string IGlbExportService.Export(string sourceGlb, string destinationGlb) => Export(sourceGlb, destinationGlb, _diagnostics);

    public GlbSceneCounts Inspect(string glbPath) => ToCounts(CanonicalGltfPipeline.Load(glbPath));

    /// <summary>SharpGLTF rewrite export (not a byte copy) with count roundtrip checks.</summary>
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
            var model = ModelRoot.Load(sourceGlb);
            model.SaveGLB(destinationGlb);
            var after = CanonicalGltfPipeline.Load(destinationGlb);
            if (after.VertexCount != before.VertexCount || after.TriangleCount != before.TriangleCount)
                throw new InvalidOperationException("GLB export changed mesh counts.");
            if (after.SkinCount != before.SkinCount || after.MorphTargetCount != before.MorphTargetCount)
                throw new InvalidOperationException("GLB export dropped skin or morph targets.");
            if (after.NodeCount != before.NodeCount || after.MaterialCount != before.MaterialCount)
                throw new InvalidOperationException("GLB export dropped nodes or materials.");
            return destinationGlb;
        }, provider: "sharpgltf-save");
    }

    public static string WriteCompleteSceneStatic(string destinationGlb, IDiagnosticService? diagnostics = null)
    {
        return PipelineTrace.Run(diagnostics, "Export", "Export.Write", () =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationGlb))!);
            var png = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGP4z8AAAAMBAQDJ/pLvAAAAAElFTkSuQmCC");
            var material = new MaterialBuilder("albedo")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, Vector4.One)
                .WithChannelImage(KnownChannel.BaseColor, new MemoryImage(png));

            var mesh = new MeshBuilder<VertexPositionNormal, VertexTexture1, VertexJoints4>("body");
            var prim = mesh.UsePrimitive(material);
            AddBox(prim, new Vector3(-0.15f, 0f, -0.15f), new Vector3(0.15f, 0.6f, 0.15f));

            var morph = mesh.UseMorphTarget(0);
            foreach (var pos in morph.Positions)
                morph.SetVertexDelta(pos, new VertexGeometryDelta(new Vector3(0, 0.08f, 0), Vector3.Zero, Vector3.Zero));

            var hips = new NodeBuilder("hips").WithLocalTranslation(Vector3.Zero);
            var spine = hips.CreateNode("spine").WithLocalTranslation(new Vector3(0, 0.3f, 0));
            var scene = new SceneBuilder();
            scene.AddSkinnedMesh(mesh, Matrix4x4.Identity, hips, spine);
            scene.ToGltf2().SaveGLB(destinationGlb);
            return destinationGlb;
        }, provider: "sharpgltf-complete-scene");
    }

    public static GlbSceneCounts ToCounts(CanonicalGltfDocument doc) => new()
    {
        MeshCount = doc.MeshCount,
        MaterialCount = doc.MaterialCount,
        TextureCount = doc.TextureCount,
        ImageCount = doc.ImageCount,
        SkinCount = doc.SkinCount,
        MorphTargetCount = doc.MorphTargetCount,
        NodeCount = doc.NodeCount,
        VertexCount = doc.VertexCount,
        TriangleCount = doc.TriangleCount,
        HasUv = doc.HasUv,
        HasJoints = doc.HasJoints
    };

    private static void AddBox(
        PrimitiveBuilder<MaterialBuilder, VertexPositionNormal, VertexTexture1, VertexJoints4> prim,
        Vector3 min,
        Vector3 max)
    {
        var p = new Vector3[]
        {
            new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z), new(max.X, max.Y, min.Z), new(min.X, max.Y, min.Z),
            new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z), new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z)
        };
        (int A, int B, int C, Vector3 N, Vector2 Ua, Vector2 Ub, Vector2 Uc)[] faces =
        [
            (0, 1, 2, new Vector3(0, 0, -1), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1)),
            (0, 2, 3, new Vector3(0, 0, -1), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1)),
            (4, 6, 5, new Vector3(0, 0, 1), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0)),
            (4, 7, 6, new Vector3(0, 0, 1), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1)),
            (0, 4, 5, new Vector3(0, -1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1)),
            (0, 5, 1, new Vector3(0, -1, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 0)),
            (3, 2, 6, new Vector3(0, 1, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1)),
            (3, 6, 7, new Vector3(0, 1, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1)),
            (0, 3, 7, new Vector3(-1, 0, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1)),
            (0, 7, 4, new Vector3(-1, 0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1)),
            (1, 5, 6, new Vector3(1, 0, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1)),
            (1, 6, 2, new Vector3(1, 0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1))
        ];
        foreach (var f in faces)
            prim.AddTriangle(V(p[f.A], f.N, f.Ua), V(p[f.B], f.N, f.Ub), V(p[f.C], f.N, f.Uc));
    }

    private static VertexBuilder<VertexPositionNormal, VertexTexture1, VertexJoints4> V(Vector3 p, Vector3 n, Vector2 uv)
    {
        var joint = p.Y > 0.3f ? 1 : 0;
        var mix = p.Y > 0.25f && p.Y < 0.4f;
        var skin = mix
            ? new VertexJoints4((0, 0.5f), (1, 0.5f))
            : new VertexJoints4((joint, 1f));
        return new(new VertexPositionNormal(p, n), new VertexTexture1(uv), skin);
    }
}
