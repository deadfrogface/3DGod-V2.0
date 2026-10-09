using ThreeDGod.AI;
using Xunit;

namespace ThreeDGodCreator.Core.Tests;

public sealed class CharacterPromptPlannerTests
{
    [Fact]
    public void DescriptiveOrcPrompt_DecomposesIntoSharedCharacterOperations()
    {
        var plans = DeterministicAiParser.ParseComposite(
            "Großer muskulöser Orc mit breitem Kiefer, Hörnern und grüner Haut");

        Assert.All(plans, p => Assert.Equal("valid", p.Status));
        Assert.Contains(plans, p => p.Operation == "parameter.delta" && p.Args.GetValueOrDefault("key") == "muscle");
        Assert.Contains(plans, p => p.Operation == "parameter.delta" && p.Args.GetValueOrDefault("key") == "jaw_width");
        Assert.Contains(plans, p => p.Operation == "parameter.delta" && p.Args.GetValueOrDefault("key") == "height");
        Assert.Contains(plans, p => p.Operation == "material.recolor" && p.Args.GetValueOrDefault("color") == "orc-green");
        Assert.Contains(plans, p => p.Operation == "creature.addPart" && p.Args.GetValueOrDefault("slot") == "horn");
    }

    [Fact]
    public void LongEditPrompt_DecomposesWithoutRegeneratingWholeCharacter()
    {
        var plans = DeterministicAiParser.ParseComposite(
            "Mach ihn größer, weniger muskulös, längere Arme, entferne die Hörner, gib ihm schwarze lange Haare und eine Lederrüstung.");

        Assert.All(plans, p => Assert.Equal("valid", p.Status));
        Assert.Contains(plans, p => p.Operation == "parameter.delta" && p.Args.GetValueOrDefault("key") == "height");
        Assert.Contains(plans, p => p.Operation == "parameter.delta" && p.Args.GetValueOrDefault("key") == "muscle" && p.Args["delta"].StartsWith("-"));
        Assert.Contains(plans, p => p.Operation == "parameter.delta" && p.Args.GetValueOrDefault("key") == "arm_length");
        Assert.Contains(plans, p => p.Operation == "creature.removePart" && p.Args.GetValueOrDefault("slot") == "horn");
        Assert.Contains(plans, p => p.Operation == "attachment.add" && p.Args.GetValueOrDefault("type") == "hair");
        Assert.Contains(plans, p => p.Operation == "garment.add" && p.Args.GetValueOrDefault("material") == "leather");
    }

    [Fact]
    public void UnknownPrompt_RemainsUnsupported_InsteadOfInventingGeometry()
    {
        var plans = DeterministicAiParser.ParseComposite("make a thing nobody mapped");
        Assert.Single(plans);
        Assert.Equal("Unsupported", plans[0].Status);
    }
}
