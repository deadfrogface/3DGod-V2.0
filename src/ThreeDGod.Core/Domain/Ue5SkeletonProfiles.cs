namespace ThreeDGod.Core.Domain;

/// <summary>
/// UE5-oriented skeleton export profiles. Extra bones are never deleted — only remapped or carried as extras.
/// </summary>
public enum Ue5SkeletonProfileKind
{
    GenericBiped,
    Ue5Humanoid,
    GenericCreature
}

public sealed class Ue5BoneMapping
{
    public string SourceBone { get; init; } = "";
    public string? TargetBone { get; init; }
    public string SemanticTag { get; init; } = "";
    public bool IsExtra { get; init; }
}

public sealed class Ue5SkeletonProfileResult
{
    public Ue5SkeletonProfileKind Profile { get; init; }
    public IReadOnlyList<Ue5BoneMapping> Mappings { get; init; } = [];
    public IReadOnlyList<string> PreservedExtraBones { get; init; } = [];
    public IReadOnlyList<string> MappedBipedBones { get; init; } = [];
}

public static class Ue5SkeletonProfiles
{
    public const string GenericBipedId = "ue5.generic-biped";
    public const string Ue5HumanoidId = "ue5.humanoid";
    public const string GenericCreatureId = "ue5.generic-creature";

    /// <summary>Epic-style humanoid bone names used as mapping targets (not a fake Mannequin mesh).</summary>
    public static readonly IReadOnlyDictionary<string, string> Ue5HumanoidTargets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [BoneSemanticTags.Hips] = "pelvis",
        [BoneSemanticTags.Spine] = "spine_01",
        [BoneSemanticTags.Chest] = "spine_03",
        [BoneSemanticTags.Neck] = "neck_01",
        [BoneSemanticTags.Head] = "head",
        [BoneSemanticTags.LeftUpperArm] = "upperarm_l",
        [BoneSemanticTags.LeftLowerArm] = "lowerarm_l",
        [BoneSemanticTags.LeftHand] = "hand_l",
        [BoneSemanticTags.RightUpperArm] = "upperarm_r",
        [BoneSemanticTags.RightLowerArm] = "lowerarm_r",
        [BoneSemanticTags.RightHand] = "hand_r",
        [BoneSemanticTags.LeftUpperLeg] = "thigh_l",
        [BoneSemanticTags.LeftLowerLeg] = "calf_l",
        [BoneSemanticTags.LeftFoot] = "foot_l",
        [BoneSemanticTags.RightUpperLeg] = "thigh_r",
        [BoneSemanticTags.RightLowerLeg] = "calf_r",
        [BoneSemanticTags.RightFoot] = "foot_r",
        [BoneSemanticTags.TailBase] = "tail_01",
        [BoneSemanticTags.WingLeft] = "wing_l",
        [BoneSemanticTags.WingRight] = "wing_r"
    };

    public static readonly IReadOnlyDictionary<string, string> GenericBipedTargets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [BoneSemanticTags.Hips] = "Hips",
        [BoneSemanticTags.Spine] = "Spine",
        [BoneSemanticTags.Chest] = "Chest",
        [BoneSemanticTags.Neck] = "Neck",
        [BoneSemanticTags.Head] = "Head",
        [BoneSemanticTags.LeftUpperArm] = "LeftUpperArm",
        [BoneSemanticTags.LeftLowerArm] = "LeftLowerArm",
        [BoneSemanticTags.LeftHand] = "LeftHand",
        [BoneSemanticTags.RightUpperArm] = "RightUpperArm",
        [BoneSemanticTags.RightLowerArm] = "RightLowerArm",
        [BoneSemanticTags.RightHand] = "RightHand",
        [BoneSemanticTags.LeftUpperLeg] = "LeftUpperLeg",
        [BoneSemanticTags.LeftLowerLeg] = "LeftLowerLeg",
        [BoneSemanticTags.LeftFoot] = "LeftFoot",
        [BoneSemanticTags.RightUpperLeg] = "RightUpperLeg",
        [BoneSemanticTags.RightLowerLeg] = "RightLowerLeg",
        [BoneSemanticTags.RightFoot] = "RightFoot"
    };

    public static Ue5SkeletonProfileResult Map(Ue5SkeletonProfileKind profile, IEnumerable<string> sourceBones)
    {
        var bones = sourceBones.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var targets = profile switch
        {
            Ue5SkeletonProfileKind.Ue5Humanoid => Ue5HumanoidTargets,
            Ue5SkeletonProfileKind.GenericBiped => GenericBipedTargets,
            _ => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) // GenericCreature: keep names, tag extras
        };

        var mappings = new List<Ue5BoneMapping>();
        var extras = new List<string>();
        var biped = new List<string>();

        foreach (var bone in bones)
        {
            var semantic = SemanticBoneMap.TryMap(bone);
            if (semantic is null)
            {
                // Unknown bone: never delete — always preserve as extra.
                mappings.Add(new Ue5BoneMapping
                {
                    SourceBone = bone,
                    TargetBone = bone,
                    SemanticTag = "extra",
                    IsExtra = true
                });
                extras.Add(bone);
                continue;
            }

            var isCreatureExtra = semantic is BoneSemanticTags.TailBase or BoneSemanticTags.WingLeft or BoneSemanticTags.WingRight
                || bone.Contains("tail", StringComparison.OrdinalIgnoreCase);

            if (profile == Ue5SkeletonProfileKind.GenericBiped && isCreatureExtra)
            {
                // Biped profile does not remap creature extras away — keep them.
                mappings.Add(new Ue5BoneMapping
                {
                    SourceBone = bone,
                    TargetBone = bone,
                    SemanticTag = semantic,
                    IsExtra = true
                });
                extras.Add(bone);
                continue;
            }

            if (profile == Ue5SkeletonProfileKind.GenericCreature)
            {
                mappings.Add(new Ue5BoneMapping
                {
                    SourceBone = bone,
                    TargetBone = bone,
                    SemanticTag = semantic,
                    IsExtra = isCreatureExtra
                });
                if (isCreatureExtra) extras.Add(bone);
                else biped.Add(bone);
                continue;
            }

            targets.TryGetValue(semantic, out var target);
            mappings.Add(new Ue5BoneMapping
            {
                SourceBone = bone,
                TargetBone = target ?? bone,
                SemanticTag = semantic,
                IsExtra = isCreatureExtra && profile != Ue5SkeletonProfileKind.Ue5Humanoid
            });
            if (isCreatureExtra)
                extras.Add(bone);
            else
                biped.Add(bone);
        }

        return new Ue5SkeletonProfileResult
        {
            Profile = profile,
            Mappings = mappings,
            PreservedExtraBones = extras,
            MappedBipedBones = biped
        };
    }

    public static Ue5SkeletonProfileKind Parse(string? profileId) => profileId switch
    {
        GenericBipedId or "GenericBiped" => Ue5SkeletonProfileKind.GenericBiped,
        Ue5HumanoidId or "UE5Humanoid" or "Ue5Humanoid" => Ue5SkeletonProfileKind.Ue5Humanoid,
        GenericCreatureId or "GenericCreature" => Ue5SkeletonProfileKind.GenericCreature,
        _ => Ue5SkeletonProfileKind.GenericCreature
    };
}
