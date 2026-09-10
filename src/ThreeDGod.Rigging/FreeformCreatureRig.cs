using System.Numerics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;

namespace ThreeDGod.Rigging;

public static class FreeformCreatureRig
{
    public static readonly string[] JointNames = ["hips", "spine", "head", "tail.base"];

    public static readonly Vector3[] JointPositions =
    [
        new(0, 0.20f, 0),
        new(0, 0.35f, 0.05f),
        new(0, 0.47f, 0.23f),
        new(0, 0.22f, -0.20f)
    ];

    public static string WriteSkinned(
        string destinationPath,
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<int> indices,
        ISkinWeightSolver? weightSolver = null)
    {
        var solver = weightSolver ?? new DistanceSkinWeightSolver();
        var hips = new NodeBuilder("hips").WithLocalTranslation(JointPositions[0]);
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
            primitive.AddTriangle(V(positions[a], solver), V(positions[b], solver), V(positions[c], solver));
        }

        var scene = new SceneBuilder();
        scene.AddSkinnedMesh(mesh, Matrix4x4.Identity, joints);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        scene.ToGltf2().SaveGLB(destinationPath);
        return destinationPath;
    }

    private static VertexBuilder<VertexPosition, VertexEmpty, VertexJoints4> V(Vector3 p, ISkinWeightSolver solver)
    {
        var weights = solver.Solve(p, JointPositions, maxInfluences: 4);
        return new(new VertexPosition(p), default, ToVertexJoints(weights));
    }

    internal static VertexJoints4 ToVertexJoints(IReadOnlyList<BoneWeight> weights)
    {
        (int, float) a = (0, 0f), b = (0, 0f), c = (0, 0f), d = (0, 0f);
        if (weights.Count > 0) a = (weights[0].BoneIndex, weights[0].Weight);
        if (weights.Count > 1) b = (weights[1].BoneIndex, weights[1].Weight);
        if (weights.Count > 2) c = (weights[2].BoneIndex, weights[2].Weight);
        if (weights.Count > 3) d = (weights[3].BoneIndex, weights[3].Weight);
        if (weights.Count == 0) a = (0, 1f);
        return new VertexJoints4(a, b, c, d);
    }
}
