using System.Numerics;
using SharpGLTF.Schema2;

namespace ThreeDGod.Rigging;

public enum TestPoseKind
{
    Arms,
    Elbow,
    Knee,
    Head,
    Squat,
    Tail
}

public sealed class TestPoseResult
{
    public TestPoseKind Pose { get; init; }
    public string JointName { get; init; } = "";
    public float MaxVertexDelta { get; init; }
    public bool Moved => MaxVertexDelta > 0.01f;
}

public static class TestPoseEvaluator
{
    public static IReadOnlyDictionary<TestPoseKind, string> PoseJoints { get; } = new Dictionary<TestPoseKind, string>
    {
        [TestPoseKind.Arms] = "upperarm.L",
        [TestPoseKind.Elbow] = "lowerarm.L",
        [TestPoseKind.Knee] = "leg.L",
        [TestPoseKind.Head] = "head",
        [TestPoseKind.Squat] = "upleg.L",
        [TestPoseKind.Tail] = "tail.base"
    };

    public static TestPoseResult Evaluate(string glbPath, TestPoseKind pose, float degrees = 55f)
    {
        var jointName = PoseJoints[pose];
        var rest = SkinAtPose(glbPath, jointName, 0);
        var posed = SkinAtPose(glbPath, jointName, degrees);
        var max = 0f;
        for (var i = 0; i < rest.Count; i++)
            max = MathF.Max(max, Vector3.Distance(rest[i], posed[i]));
        return new TestPoseResult { Pose = pose, JointName = jointName, MaxVertexDelta = max };
    }

    public static List<Vector3> SkinAtPose(string glbPath, string jointName, float degrees)
    {
        var model = ModelRoot.Load(glbPath);
        var skin = model.LogicalSkins[0];
        var worlds = new Matrix4x4[skin.JointsCount];
        var ibms = new Matrix4x4[skin.JointsCount];
        for (var i = 0; i < skin.JointsCount; i++)
        {
            var joint = skin.GetJoint(i);
            ibms[i] = joint.InverseBindMatrix;
            worlds[i] = WorldOf(joint.Joint, jointName, degrees);
        }

        var primitive = model.LogicalMeshes.SelectMany(m => m.Primitives).First();
        var positions = primitive.GetVertexAccessor("POSITION")!.AsVector3Array();
        var joints = primitive.GetVertexAccessor("JOINTS_0")!.AsVector4Array();
        var weights = primitive.GetVertexAccessor("WEIGHTS_0")!.AsVector4Array();
        var skinned = new List<Vector3>(positions.Count);
        for (var i = 0; i < positions.Count; i++)
        {
            var p = new Vector4(positions[i], 1);
            var acc = Vector4.Zero;
            Accumulate(ref acc, p, joints[i].X, weights[i].X, worlds, ibms);
            Accumulate(ref acc, p, joints[i].Y, weights[i].Y, worlds, ibms);
            Accumulate(ref acc, p, joints[i].Z, weights[i].Z, worlds, ibms);
            Accumulate(ref acc, p, joints[i].W, weights[i].W, worlds, ibms);
            skinned.Add(new Vector3(acc.X, acc.Y, acc.Z));
        }
        return skinned;
    }

    private static void Accumulate(ref Vector4 acc, Vector4 rest, float joint, float weight, Matrix4x4[] worlds, Matrix4x4[] ibms)
    {
        if (weight <= 0)
            return;
        var index = (int)MathF.Round(joint);
        acc += weight * Vector4.Transform(rest, ibms[index] * worlds[index]);
    }

    private static Matrix4x4 WorldOf(Node node, string rotateName, float degrees)
    {
        var chain = new List<Node>();
        for (var current = node; current is not null; current = current.VisualParent)
            chain.Add(current);
        chain.Reverse();
        var world = Matrix4x4.Identity;
        foreach (var step in chain)
        {
            var local = step.LocalMatrix;
            if (string.Equals(step.Name, rotateName, StringComparison.OrdinalIgnoreCase) && degrees != 0)
            {
                var axis = rotateName.Contains("leg", StringComparison.OrdinalIgnoreCase) ||
                           rotateName.Contains("tail", StringComparison.OrdinalIgnoreCase)
                    ? Vector3.UnitX
                    : Vector3.UnitZ;
                local *= Matrix4x4.CreateFromAxisAngle(axis, degrees * MathF.PI / 180f);
            }
            world *= local;
        }
        return world;
    }
}
