using System.Numerics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;

namespace ThreeDGod.Mesh;

public static class TriangleMeshExport
{
    public static void WriteGlb(string destinationPath, IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices) =>
        WriteGlb(destinationPath, positions, indices, new Vector4(0.82f, 0.64f, 0.52f, 1), metallic: 0, roughness: 0.8f);

    public static void WriteGlb(
        string destinationPath,
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<int> indices,
        Vector4 baseColor,
        float metallic,
        float roughness)
    {
        if (positions.Count < 3 || indices.Count < 3)
            throw new InvalidOperationException("Mesh has no triangles.");

        var material = new MaterialBuilder("pbr")
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader()
            .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, baseColor)
            .WithMetallicRoughness(metallic, roughness);

        var mesh = new MeshBuilder<VertexPosition>("human");
        var primitive = mesh.UsePrimitive(material);
        for (var i = 0; i + 2 < indices.Count; i += 3)
        {
            var a = indices[i];
            var b = indices[i + 1];
            var c = indices[i + 2];
            if (a < 0 || b < 0 || c < 0 || a >= positions.Count || b >= positions.Count || c >= positions.Count)
                throw new InvalidOperationException("Invalid triangle index while writing GLB.");
            primitive.AddTriangle(
                new VertexPosition(positions[a]),
                new VertexPosition(positions[b]),
                new VertexPosition(positions[c]));
        }

        var scene = new SceneBuilder();
        scene.AddRigidMesh(mesh, Matrix4x4.Identity);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        scene.ToGltf2().SaveGLB(destinationPath);
    }

    public static string ObjToGlb(string objPath, string destinationPath)
    {
        var imported = ObjImporter.ImportObj(objPath);
        WriteGlb(destinationPath, imported.Positions, imported.Indices);
        return destinationPath;
    }
}
