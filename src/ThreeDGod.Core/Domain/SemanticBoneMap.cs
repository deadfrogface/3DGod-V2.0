namespace ThreeDGod.Core.Domain;

/// <summary>
/// Maps common / Anny-style bone labels onto semantic tags. Does not invent a SkinTokens rig.
/// </summary>
public static class SemanticBoneMap
{
    public static string? TryMap(string boneName)
    {
        if (string.IsNullOrWhiteSpace(boneName))
            return null;
        var n = boneName.Trim().ToLowerInvariant().Replace(' ', '.');
        return n switch
        {
            "root" or "hips" or SemanticSlots.Hip => BoneSemanticTags.Hips,
            "spine" => BoneSemanticTags.Spine,
            "chest" or SemanticSlots.Chest => BoneSemanticTags.Chest,
            "neck" or SemanticSlots.Neck => BoneSemanticTags.Neck,
            "head" => BoneSemanticTags.Head,
            "ear.l" or "ear.r" or "nose" => BoneSemanticTags.Head,
            "upperarm.l" or "arm.l" or "shoulder.l" => BoneSemanticTags.LeftUpperArm,
            "lowerarm.l" or "forearm.l" or SemanticSlots.WristLeft => BoneSemanticTags.LeftLowerArm,
            "hand.l" or SemanticSlots.HandLeft => BoneSemanticTags.LeftHand,
            "upperarm.r" or "arm.r" or "shoulder.r" => BoneSemanticTags.RightUpperArm,
            "lowerarm.r" or "forearm.r" or SemanticSlots.WristRight => BoneSemanticTags.RightLowerArm,
            "hand.r" or SemanticSlots.HandRight => BoneSemanticTags.RightHand,
            "upleg.l" or "thigh.l" => BoneSemanticTags.LeftUpperLeg,
            "leg.l" or "shin.l" => BoneSemanticTags.LeftLowerLeg,
            "foot.l" => BoneSemanticTags.LeftFoot,
            "upleg.r" or "thigh.r" => BoneSemanticTags.RightUpperLeg,
            "leg.r" or "shin.r" => BoneSemanticTags.RightLowerLeg,
            "foot.r" => BoneSemanticTags.RightFoot,
            "tail" or "tail.base" => BoneSemanticTags.TailBase,
            "wing.l" => BoneSemanticTags.WingLeft,
            "wing.r" => BoneSemanticTags.WingRight,
            _ => null
        };
    }

    public static IReadOnlyDictionary<string, string> MapAll(IEnumerable<string> boneNames)
    {
        var mapped = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in boneNames)
        {
            var tag = TryMap(name);
            if (tag is not null)
                mapped[name] = tag;
        }
        return mapped;
    }
}
