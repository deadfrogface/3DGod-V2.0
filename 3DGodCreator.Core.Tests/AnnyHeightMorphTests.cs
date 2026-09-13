using ThreeDGod.Application;
using ThreeDGod.Core.Domain;

namespace ThreeDGodCreator.Core.Tests;

public class AnnyHeightMorphTests
{
    [Fact]
    public void ResolvePhenotypeKey_PrefersLocalProportionOverGlobalHeight()
    {
        var key = AnnyHeightMorph.ResolvePhenotypeKey(["weight", "height", "age"], ["torso_length", "smile"]);
        Assert.Equal("torso_length", key);
    }

    [Fact]
    public void ResolvePhenotypeKey_FallsBackToHeightWhenNoLocalProportion()
    {
        var key = AnnyHeightMorph.ResolvePhenotypeKey(["weight", "height", "age"], ["smile"]);
        Assert.Equal("height", key);
    }

    [Fact]
    public void ApplyTaller_UpdatesPhenotype_NotUniformScaleFlag()
    {
        var human = new ParametricHumanState();
        var key = AnnyHeightMorph.ResolvePhenotypeKey(["height"]);
        AnnyHeightMorph.ApplyTaller(human, 0.2f, key);
        Assert.True(human.PhenotypeParameters[key] > 0);
        Assert.True(human.PhenotypeParameters[AnnyHeightMorph.ProductParameterKey] > 0);
    }

    [Fact]
    public void LooksLikeNonUniformHeightChange_RejectsUniformScale()
    {
        Assert.False(AnnyHeightMorph.LooksLikeNonUniformHeightChange(1f, 1.2f, 1f, 1.2f));
        Assert.True(AnnyHeightMorph.LooksLikeNonUniformHeightChange(1f, 1.15f, 1f, 1.02f));
    }

    [Fact]
    public void IsProportionLocalKey_DetectsTorsoAndLeg()
    {
        Assert.True(AnnyHeightMorph.IsProportionLocalKey("torso_length"));
        Assert.True(AnnyHeightMorph.IsProportionLocalKey("left_leg"));
        Assert.False(AnnyHeightMorph.IsProportionLocalKey("smile"));
        Assert.False(AnnyHeightMorph.IsProportionLocalKey("hair_length"));
    }
}
