using System.Numerics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using ThreeDGod.Mesh;

namespace ThreeDGod.Rigging;

public static class HumanoidTestRig
{
    public static readonly string[] JointNames =
    [
        "hips", "spine", "chest", "neck", "head",
        "upperarm.L", "lowerarm.L", "hand.L",
        "upperarm.R", "lowerarm.R", "hand.R",
        "upleg.L", "leg.L", "foot.L",
        "upleg.R", "leg.R", "foot.R",
        "tail.base"
    ];

    public static string WriteGood(string destinationPath)
    {
        var hips = Node("hips", new Vector3(0, 1f, 0));
        var spine = Child(hips, "spine", new Vector3(0, 0.18f, 0));
        var chest = Child(spine, "chest", new Vector3(0, 0.18f, 0));
        var neck = Child(chest, "neck", new Vector3(0, 0.16f, 0));
        var head = Child(neck, "head", new Vector3(0, 0.16f, 0));
        var upperL = Child(chest, "upperarm.L", new Vector3(-0.18f, 0.05f, 0));
        var lowerL = Child(upperL, "lowerarm.L", new Vector3(-0.22f, 0, 0));
        var handL = Child(lowerL, "hand.L", new Vector3(-0.16f, 0, 0));
        var upperR = Child(chest, "upperarm.R", new Vector3(0.18f, 0.05f, 0));
        var lowerR = Child(upperR, "lowerarm.R", new Vector3(0.22f, 0, 0));
        var handR = Child(lowerR, "hand.R", new Vector3(0.16f, 0, 0));
        var upL = Child(hips, "upleg.L", new Vector3(-0.1f, -0.05f, 0));
        var legL = Child(upL, "leg.L", new Vector3(0, -0.38f, 0));
        var footL = Child(legL, "foot.L", new Vector3(0, -0.38f, 0.06f));
        var upR = Child(hips, "upleg.R", new Vector3(0.1f, -0.05f, 0));
        var legR = Child(upR, "leg.R", new Vector3(0, -0.38f, 0));
        var footR = Child(legR, "foot.R", new Vector3(0, -0.38f, 0.06f));
        var tail = Child(hips, "tail.base", new Vector3(0, 0.02f, -0.12f));

        NodeBuilder[] joints =
        [
            hips, spine, chest, neck, head,
            upperL, lowerL, handL,
            upperR, lowerR, handR,
            upL, legL, footL,
            upR, legR, footR,
            tail
        ];

        var material = new MaterialBuilder("rig-test")
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader();
        var mesh = new MeshBuilder<VertexPosition, VertexEmpty, VertexJoints4>("humanoid");
        var primitive = mesh.UsePrimitive(material);

        AddWeightedBox(primitive, new Vector3(-0.12f, 0.92f, -0.08f), new Vector3(0.12f, 1.12f, 0.08f), 0);
        AddWeightedBox(primitive, new Vector3(-0.1f, 1.12f, -0.07f), new Vector3(0.1f, 1.28f, 0.07f), 1);
        AddWeightedBox(primitive, new Vector3(-0.14f, 1.28f, -0.08f), new Vector3(0.14f, 1.48f, 0.08f), 2);
        AddWeightedBox(primitive, new Vector3(-0.05f, 1.48f, -0.05f), new Vector3(0.05f, 1.6f, 0.05f), 3);
        AddWeightedBox(primitive, new Vector3(-0.1f, 1.6f, -0.1f), new Vector3(0.1f, 1.82f, 0.1f), 4);
        AddWeightedBox(primitive, new Vector3(-0.4f, 1.36f, -0.04f), new Vector3(-0.18f, 1.46f, 0.04f), 5);
        AddWeightedBox(primitive, new Vector3(-0.62f, 1.36f, -0.035f), new Vector3(-0.4f, 1.45f, 0.035f), 6);
        AddWeightedBox(primitive, new Vector3(-0.78f, 1.35f, -0.03f), new Vector3(-0.62f, 1.44f, 0.03f), 7);
        AddWeightedBox(primitive, new Vector3(0.18f, 1.36f, -0.04f), new Vector3(0.4f, 1.46f, 0.04f), 8);
        AddWeightedBox(primitive, new Vector3(0.4f, 1.36f, -0.035f), new Vector3(0.62f, 1.45f, 0.035f), 9);
        AddWeightedBox(primitive, new Vector3(0.62f, 1.35f, -0.03f), new Vector3(0.78f, 1.44f, 0.03f), 10);
        AddWeightedBox(primitive, new Vector3(-0.16f, 0.58f, -0.06f), new Vector3(-0.04f, 0.98f, 0.06f), 11);
        AddWeightedBox(primitive, new Vector3(-0.15f, 0.2f, -0.05f), new Vector3(-0.05f, 0.58f, 0.05f), 12);
        AddWeightedBox(primitive, new Vector3(-0.16f, 0.0f, -0.04f), new Vector3(-0.02f, 0.1f, 0.12f), 13);
        AddWeightedBox(primitive, new Vector3(0.04f, 0.58f, -0.06f), new Vector3(0.16f, 0.98f, 0.06f), 14);
        AddWeightedBox(primitive, new Vector3(0.05f, 0.2f, -0.05f), new Vector3(0.15f, 0.58f, 0.05f), 15);
        AddWeightedBox(primitive, new Vector3(0.02f, 0.0f, -0.04f), new Vector3(0.16f, 0.1f, 0.12f), 16);
        AddWeightedBox(primitive, new Vector3(-0.03f, 0.96f, -0.32f), new Vector3(0.03f, 1.04f, -0.12f), 17);

        var scene = new SceneBuilder();
        scene.AddSkinnedMesh(mesh, Matrix4x4.Identity, joints);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        scene.ToGltf2().SaveGLB(destinationPath);
        return destinationPath;
    }

    public static string WriteUnskinned(string destinationPath)
    {
        var builder = new MeshBuilder3D();
        builder.AddSphere(Vector3.Zero, 0.5f, 12, 8);
        TriangleMeshExport.WriteGlb(destinationPath, builder.Positions, builder.Indices);
        return destinationPath;
    }

    private static NodeBuilder Node(string name, Vector3 world) =>
        new NodeBuilder(name).WithLocalTranslation(world);

    private static NodeBuilder Child(NodeBuilder parent, string name, Vector3 local) =>
        parent.CreateNode(name).WithLocalTranslation(local);

    private static void AddWeightedBox(
        IPrimitiveBuilder primitive,
        Vector3 min,
        Vector3 max,
        int joint)
    {
        var p = new Vector3[]
        {
            new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z), new(max.X, max.Y, min.Z), new(min.X, max.Y, min.Z),
            new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z), new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z)
        };
        int[] tris =
        [
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6,
            0, 4, 5, 0, 5, 1,
            3, 2, 6, 3, 6, 7,
            0, 3, 7, 0, 7, 4,
            1, 5, 6, 1, 6, 2
        ];
        for (var i = 0; i < tris.Length; i += 3)
            primitive.AddTriangle(V(p[tris[i]], joint), V(p[tris[i + 1]], joint), V(p[tris[i + 2]], joint));
    }

    private static VertexBuilder<VertexPosition, VertexEmpty, VertexJoints4> V(Vector3 p, int joint) =>
        new(new VertexPosition(p), default, new VertexJoints4((joint, 1f)));
}
