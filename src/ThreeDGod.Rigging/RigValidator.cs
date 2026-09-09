using System.Numerics;
using SharpGLTF.Schema2;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Rigging;

public sealed class RigIssue
{
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed class RigValidationReport
{
    public bool Passed => Failures.Count == 0;
    public List<RigIssue> Failures { get; } = [];
    public List<string> JointNames { get; init; } = [];
    public int VertexCount { get; set; }
    public int SkinCount { get; set; }
}

public static class RigValidator
{
    public static readonly string[] HumanoidRequired =
    [
        BoneSemanticTags.Hips, BoneSemanticTags.Neck, BoneSemanticTags.Head,
        BoneSemanticTags.LeftHand, BoneSemanticTags.RightHand,
        BoneSemanticTags.LeftFoot, BoneSemanticTags.RightFoot
    ];

    public static RigValidationReport ValidateGlb(string glbPath, bool requireHumanoid = true)
    {
        var report = new RigValidationReport();
        var model = ModelRoot.Load(glbPath);
        report.SkinCount = model.LogicalSkins.Count;
        if (model.LogicalSkins.Count == 0)
        {
            report.Failures.Add(new RigIssue { Code = "NoSkin", Message = "GLB has no skin." });
            return report;
        }

        var names = new List<string>();
        foreach (var skin in model.LogicalSkins)
        {
            if (skin.JointsCount == 0)
                report.Failures.Add(new RigIssue { Code = "NoJoints", Message = "Skin has no joints." });
            for (var i = 0; i < skin.JointsCount; i++)
            {
                var joint = skin.GetJoint(i);
                names.Add(joint.Joint.Name ?? $"joint{i}");
                var ibm = joint.InverseBindMatrix;
                if (!IsFinite(ibm))
                    report.Failures.Add(new RigIssue { Code = "NonFiniteBind", Message = $"IBM of {joint.Joint.Name} is not finite." });
            }
        }
        report.JointNames.AddRange(names);

        var parentCycle = DetectNodeCycle(model);
        if (parentCycle)
            report.Failures.Add(new RigIssue { Code = "HierarchyCycle", Message = "Node parent graph has a cycle." });

        var vertices = 0;
        foreach (var primitive in model.LogicalMeshes.SelectMany(m => m.Primitives))
        {
            var pos = primitive.GetVertexAccessor("POSITION");
            var joints = primitive.GetVertexAccessor("JOINTS_0");
            var weights = primitive.GetVertexAccessor("WEIGHTS_0");
            if (pos is null)
                continue;
            vertices += pos.Count;
            if (joints is null || weights is null)
            {
                report.Failures.Add(new RigIssue { Code = "NoSkinAttributes", Message = "Primitive missing JOINTS_0/WEIGHTS_0." });
                continue;
            }

            var jointCount = model.LogicalSkins[0].JointsCount;
            var j = joints.AsVector4Array();
            var w = weights.AsVector4Array();
            for (var i = 0; i < pos.Count; i++)
            {
                var sum = w[i].X + w[i].Y + w[i].Z + w[i].W;
                if (sum < 0.95f || sum > 1.05f)
                {
                    report.Failures.Add(new RigIssue { Code = "WeightSum", Message = $"Vertex {i} weight sum {sum} is not 1." });
                    break;
                }
                if (OutOfRange(j[i], jointCount))
                {
                    report.Failures.Add(new RigIssue { Code = "InvalidJointIndex", Message = $"Vertex {i} references a missing joint." });
                    break;
                }
            }
        }
        report.VertexCount = vertices;

        if (requireHumanoid)
        {
            var mapped = SemanticBoneMap.MapAll(names);
            foreach (var required in HumanoidRequired)
            {
                if (!mapped.Values.Contains(required))
                    report.Failures.Add(new RigIssue { Code = "MissingSemantic", Message = $"Missing required bone {required}." });
            }
        }

        return report;
    }

    public static RigValidationReport ValidateHierarchy(IReadOnlyList<BoneDefinition> bones)
    {
        var report = new RigValidationReport { JointNames = bones.Select(b => b.Name).ToList() };
        var byId = bones.ToDictionary(b => b.BoneId);
        var visiting = new HashSet<string>();
        var seen = new HashSet<string>();
        foreach (var bone in bones)
        {
            visiting.Clear();
            var current = bone.BoneId;
            while (current is not null)
            {
                if (!visiting.Add(current))
                {
                    report.Failures.Add(new RigIssue { Code = "HierarchyCycle", Message = $"Cycle at {bone.Name}." });
                    return report;
                }
                if (!byId.TryGetValue(current, out var node))
                {
                    report.Failures.Add(new RigIssue { Code = "MissingParent", Message = $"Parent of {bone.Name} is missing." });
                    break;
                }
                current = node.ParentBoneId;
            }
            seen.Add(bone.BoneId);
        }
        return report;
    }

    private static bool DetectNodeCycle(ModelRoot model)
    {
        foreach (var node in model.LogicalNodes)
        {
            var seen = new HashSet<int>();
            var current = node;
            while (current.VisualParent is { } parent)
            {
                if (!seen.Add(current.LogicalIndex))
                    return true;
                current = parent;
            }
        }
        return false;
    }

    private static bool OutOfRange(Vector4 joints, int jointCount) =>
        IndexBad(joints.X, jointCount) || IndexBad(joints.Y, jointCount) ||
        IndexBad(joints.Z, jointCount) || IndexBad(joints.W, jointCount);

    private static bool IndexBad(float joint, int jointCount)
    {
        var index = (int)MathF.Round(joint);
        return index < 0 || index >= jointCount;
    }

    private static bool IsFinite(Matrix4x4 m) =>
        float.IsFinite(m.M11) && float.IsFinite(m.M12) && float.IsFinite(m.M13) && float.IsFinite(m.M14) &&
        float.IsFinite(m.M21) && float.IsFinite(m.M22) && float.IsFinite(m.M23) && float.IsFinite(m.M24) &&
        float.IsFinite(m.M31) && float.IsFinite(m.M32) && float.IsFinite(m.M33) && float.IsFinite(m.M34) &&
        float.IsFinite(m.M41) && float.IsFinite(m.M42) && float.IsFinite(m.M43) && float.IsFinite(m.M44);
}
