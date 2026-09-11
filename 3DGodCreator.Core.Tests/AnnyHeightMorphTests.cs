using ThreeDGod.Application;
using ThreeDGod.Core.Domain;

namespace ThreeDGodCreator.Core.Tests;

public class AnnyHeightMorphTests
{
    [Fact]
    public void ResolvePhenotypeKey_PrefersExplicitHeightLabel()
    {
        var key = AnnyHeightMorph.ResolvePhenotypeKey(["weight", "height", "age"], ["torso"]);
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
}
