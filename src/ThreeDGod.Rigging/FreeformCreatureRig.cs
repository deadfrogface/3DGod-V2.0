using System.Numerics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;

namespace ThreeDGod.Rigging;

public static class FreeformCreatureRig
{
    public static readonly string[] JointNames = ["hips", "spine", "head", "tail.base"];

    public static string WriteSkinned(
        string destinationPath,
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<int> indices)
    {
        var hips = new NodeBuilder("hips").WithLocalTranslation(new Vector3(0, 0.2f, 0));
        var spine = hips.CreateNode("spine").WithLocalTranslation(new Vector3(0, 0.15f, 0.05f));
        var head = spine.CreateNode("head").WithLocalTranslation(new Vector3(0, 0.12f, 0.18f));
        var tail = hips.CreateNode("tail.base").WithLocalTranslation(new Vector3(0, 0.02f, -0.2f));
        NodeBuilder[] joints = [hips, spine, head, tail];

        var material = new MaterialBuilder("freeform")
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader();
        var mesh = new MeshBuilder<VertexPosition, VertexEmpty, VertexJoints4>("freeform");
        var primitive = mesh.UsePrimitive(material);
        for (var i = 0; i + 2 < indices.Count; i += 3)
        {
            var a = indices[i];
            var b = indices[i + 1];
            var c = indices[i + 2];
            primitive.AddTriangle(V(positions[a]), V(positions[b]), V(positions[c]));
        }

        var scene = new SceneBuilder();
        scene.AddSkinnedMesh(mesh, Matrix4x4.Identity, joints);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        scene.ToGltf2().SaveGLB(destinationPath);
        return destinationPath;
    }

    private static VertexBuilder<VertexPosition, VertexEmpty, VertexJoints4> V(Vector3 p)
    {
        var joint = 0;
        if (p.Z < -0.12f)
            joint = 3;
        else if (p.Y > 0.38f || p.Z > 0.22f)
            joint = 2;
        else if (p.Y > 0.22f)
            joint = 1;
        return new(new VertexPosition(p), default, new VertexJoints4((joint, 1f)));
    }
}
