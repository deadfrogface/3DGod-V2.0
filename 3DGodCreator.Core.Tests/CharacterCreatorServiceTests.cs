using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;
using Xunit;

namespace ThreeDGodCreator.Core.Tests;

public sealed class CharacterCreatorServiceTests
{
    [Fact]
    public void OrcPreset_IsHumanoidCreature_AndHasIndependentTusks()
    {
        var service = new CharacterCreatorService();
        var orc = service.Create("orc");

        Assert.Equal(CharacterBaseKind.HumanoidCreature, orc.BaseKind);
        Assert.Equal("anny", orc.BaseProvider);
        Assert.Equal("orc", orc.Species);
        Assert.Contains("tusk_left", orc.Parts.Keys);
        Assert.Contains("tusk_right", orc.Parts.Keys);
    }

    [Fact]
    public void Create_ReturnsIndependentPresetCopies()
    {
        var service = new CharacterCreatorService();
        var first = service.Create("orc");
        service.SetMorph(first, "jaw_width", 1.0);
        service.SetPart(first, "tusk_left", CharacterPartKind.CreaturePart, null);

        var second = service.Create("orc");

        Assert.Equal(0.55, second.Morphs["jaw_width"], 3);
        Assert.Contains("tusk_left", second.Parts.Keys);
    }

    [Fact]
    public void Morphs_AreFiniteAndClamped()
    {
        var service = new CharacterCreatorService();
        var character = service.Create("human");

        service.SetMorph(character, "height", 5.0);
        Assert.Equal(1.0, character.Morphs["height"]);

        service.AddMorphDelta(character, "height", -4.0);
        Assert.Equal(-1.0, character.Morphs["height"]);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.SetMorph(character, "height", double.NaN));
    }

    [Fact]
    public void Parts_CanBeAddedReplacedAndRemovedBySlot()
    {
        var service = new CharacterCreatorService();
        var character = service.Create("human");

        service.SetPart(character, "hair", CharacterPartKind.Hair, "hair_short_01");
        Assert.Equal("hair_short_01", character.Parts["hair"].AssetId);

        service.SetPart(character, "hair", CharacterPartKind.Hair, "hair_long_01");
        Assert.Equal("hair_long_01", character.Parts["hair"].AssetId);

        service.SetPart(character, "hair", CharacterPartKind.Hair, null);
        Assert.DoesNotContain("hair", character.Parts.Keys);
    }
}
