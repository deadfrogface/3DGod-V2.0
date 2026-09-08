namespace ThreeDGod.Core.Domain;

public sealed class RigDefinition : DomainDocument
{
    public Guid RigId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string SkeletonProfileId { get; set; } = "";
    public List<BoneDefinition> Bones { get; set; } = [];
    public string? SkinBindingReference { get; set; }
    public string? RootBoneId { get; set; }
    public bool IsHumanoid { get; set; }
    public string ValidationState { get; set; } = "unknown";
    public string? GeneratorProvenance { get; set; }
}

public sealed class BoneDefinition
{
    public string BoneId { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string? ParentBoneId { get; set; }
    public SpatialTransform LocalBindTransform { get; set; } = new();
    public double[] InverseBindMatrix { get; set; } = Identity4x4();
    public List<string> SemanticTags { get; set; } = [];
    public string? Constraints { get; set; }

    public static double[] Identity4x4() =>
    [
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    ];
}

public static class BoneSemanticTags
{
    public const string Root = "Root";
    public const string Hips = "Hips";
    public const string Spine = "Spine";
    public const string Chest = "Chest";
    public const string Neck = "Neck";
    public const string Head = "Head";
    public const string LeftUpperArm = "LeftUpperArm";
    public const string LeftLowerArm = "LeftLowerArm";
    public const string LeftHand = "LeftHand";
    public const string RightUpperArm = "RightUpperArm";
    public const string RightLowerArm = "RightLowerArm";
    public const string RightHand = "RightHand";
    public const string LeftUpperLeg = "LeftUpperLeg";
    public const string LeftLowerLeg = "LeftLowerLeg";
    public const string LeftFoot = "LeftFoot";
    public const string RightUpperLeg = "RightUpperLeg";
    public const string RightLowerLeg = "RightLowerLeg";
    public const string RightFoot = "RightFoot";
    public const string TailBase = "TailBase";
    public const string WingLeft = "WingLeft";
    public const string WingRight = "WingRight";
    public const string Custom = "Custom";
}
