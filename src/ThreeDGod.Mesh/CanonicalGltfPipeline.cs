using SharpGLTF.Schema2;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Mesh;

public sealed class CanonicalGltfDocument
{
    public int MeshCount { get; init; }
    public int PrimitiveCount { get; init; }
    public int MaterialCount { get; init; }
    public int SkinCount { get; init; }
    public int MorphTargetCount { get; init; }
    public int NodeCount { get; init; }
    public int TextureCount { get; init; }
    public int ImageCount { get; init; }
    public int VertexCount { get; init; }
    public int TriangleCount { get; init; }
    public bool HasNormals { get; init; }
    public bool HasTangents { get; init; }
    public bool HasUv { get; init; }
    public bool HasJoints { get; init; }
}

public static class CanonicalGltfPipeline
{
    public static CanonicalGltfDocument Load(string path)
    {
        var model = ModelRoot.Load(path);
        return Inspect(model);
    }

    public static CanonicalGltfDocument Roundtrip(string sourcePath, string destinationPath)
    {
        var model = ModelRoot.Load(sourcePath);
        model.SaveGLB(destinationPath);
        var reloaded = ModelRoot.Load(destinationPath);
        return Inspect(reloaded);
    }

    public static CanonicalGltfDocument Inspect(ModelRoot model)
    {
        var primitives = model.LogicalMeshes.SelectMany(m => m.Primitives).ToList();
        var vertices = 0;
        var tris = 0;
        var morphs = 0;
        var hasN = false;
        var hasT = false;
        var hasUv = false;
        var hasJoints = false;
        foreach (var p in primitives)
        {
            var pos = p.GetVertexAccessor("POSITION");
            if (pos != null)
                vertices += pos.Count;
            var idx = p.GetIndexAccessor();
            if (idx != null)
                tris += idx.Count / 3;
            morphs += p.MorphTargetsCount;
            hasN |= p.GetVertexAccessor("NORMAL") != null;
            hasT |= p.GetVertexAccessor("TANGENT") != null;
            hasUv |= p.GetVertexAccessor("TEXCOORD_0") != null;
            hasJoints |= p.GetVertexAccessor("JOINTS_0") != null;
        }

        return new CanonicalGltfDocument
        {
            MeshCount = model.LogicalMeshes.Count,
            PrimitiveCount = primitives.Count,
            MaterialCount = model.LogicalMaterials.Count,
            SkinCount = model.LogicalSkins.Count,
            MorphTargetCount = morphs,
            NodeCount = model.LogicalNodes.Count,
            TextureCount = model.LogicalTextures.Count,
            ImageCount = model.LogicalImages.Count,
            VertexCount = vertices,
            TriangleCount = tris,
            HasNormals = hasN,
            HasTangents = hasT,
            HasUv = hasUv,
            HasJoints = hasJoints
        };
    }

    public static MeshAsset ToMeshAsset(string name, string path, CanonicalGltfDocument doc) =>
        new()
        {
            Name = name,
            SourceFormat = "glb",
            CanonicalGlbPath = path,
            VertexCount = doc.VertexCount,
            TriangleCount = doc.TriangleCount,
            UvSetCount = doc.HasUv ? 1 : 0,
            HasNormals = doc.HasNormals,
            HasTangents = doc.HasTangents,
            HasSkin = doc.SkinCount > 0,
            HasMorphTargets = doc.MorphTargetCount > 0,
            ValidationState = "loaded"
        };
}
