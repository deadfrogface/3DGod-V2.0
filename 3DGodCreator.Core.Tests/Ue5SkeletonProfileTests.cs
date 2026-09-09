using ThreeDGod.Core.Domain;

namespace ThreeDGodCreator.Core.Tests;

public class Ue5SkeletonProfileTests
{
    [Fact]
    public void GenericBiped_MapsCoreBones_AndKeepsRatTail()
    {
        var bones = new[]
        {
            "hips", "spine", "neck", "head",
            "upperarm.l", "lowerarm.l", "hand.l",
            "upperarm.r", "lowerarm.r", "hand.r",
            "upleg.l", "leg.l", "foot.l",
            "upleg.r", "leg.r", "foot.r",
            "tail.base", "tail.mid", "weapon.bone"
        };

        var result = Ue5SkeletonProfiles.Map(Ue5SkeletonProfileKind.GenericBiped, bones);
        Assert.Contains(result.MappedBipedBones, b => b.Equals("hips", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.PreservedExtraBones, b => b.Equals("tail.base", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.PreservedExtraBones, b => b.Equals("tail.mid", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.PreservedExtraBones, b => b.Equals("weapon.bone", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(bones.Length, result.Mappings.Count); // nothing deleted
        Assert.All(result.Mappings.Where(m => m.SourceBone.Contains("tail", StringComparison.OrdinalIgnoreCase)),
            m => Assert.True(m.IsExtra));
    }

    [Fact]
    public void Ue5Humanoid_MapsPelvisAndSpine01()
    {
        var result = Ue5SkeletonProfiles.Map(Ue5SkeletonProfileKind.Ue5Humanoid, ["hips", "spine", "tail.base"]);
        Assert.Equal("pelvis", result.Mappings.Single(m => m.SourceBone == "hips").TargetBone);
        Assert.Equal("spine_01", result.Mappings.Single(m => m.SourceBone == "spine").TargetBone);
        Assert.Equal("tail_01", result.Mappings.Single(m => m.SourceBone == "tail.base").TargetBone);
        Assert.Contains("tail.base", result.PreservedExtraBones);
    }

    [Fact]
    public void GenericCreature_KeepsSourceNames_AndMarksExtras()
    {
        var result = Ue5SkeletonProfiles.Map(Ue5SkeletonProfileKind.GenericCreature, ["hips", "tail.base", "wing.l"]);
        Assert.Equal("hips", result.Mappings.Single(m => m.SourceBone == "hips").TargetBone);
        Assert.Contains("tail.base", result.PreservedExtraBones);
        Assert.Contains("wing.l", result.PreservedExtraBones);
        Assert.DoesNotContain(result.Mappings, m => m.TargetBone is null);
    }

    [Fact]
    public void Parse_RecognizesProfileIds()
    {
        Assert.Equal(Ue5SkeletonProfileKind.GenericBiped, Ue5SkeletonProfiles.Parse(Ue5SkeletonProfiles.GenericBipedId));
        Assert.Equal(Ue5SkeletonProfileKind.Ue5Humanoid, Ue5SkeletonProfiles.Parse("UE5Humanoid"));
        Assert.Equal(Ue5SkeletonProfileKind.GenericCreature, Ue5SkeletonProfiles.Parse(Ue5SkeletonProfiles.GenericCreatureId));
    }
}
