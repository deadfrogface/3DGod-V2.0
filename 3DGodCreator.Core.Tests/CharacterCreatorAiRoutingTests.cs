using ThreeDGod.AI;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure;
using Xunit;

namespace ThreeDGodCreator.Core.Tests;

public sealed class CharacterCreatorAiRoutingTests
{
    [Theory]
    [InlineData("muscle", 0.25f)]
    [InlineData("weight", -0.20f)]
    [InlineData("proportions", 0.15f)]
    public async Task ParameterDelta_UpdatesAuthoritativeAnnyPhenotype(string key, float delta)
    {
        var session = new ActiveProjectSession();
        var executor = new AllowlistedAiEditExecutor(session);
        var result = await executor.ExecutePlanAsync(new AiEditPlan
        {
            Status = "valid",
            Operation = "parameter.delta",
            Args = { ["key"] = key, ["delta"] = delta.ToString(System.Globalization.CultureInfo.InvariantCulture) }
        });

        Assert.True(result.Ok, result.Message);
        var state = session.ActiveCharacter!.ParametricHumanState!;
        Assert.Equal(Math.Clamp(0.5f + delta, 0f, 1f), state.PhenotypeParameters[key], 3);
        Assert.True(session.IsDirty);
    }

    [Theory]
    [InlineData("jaw_width")]
    [InlineData("brow_ridge")]
    [InlineData("nose_width")]
    [InlineData("shoulder_width")]
    public async Task ParameterDelta_UpdatesAuthoritativeLocalShape(string key)
    {
        var session = new ActiveProjectSession();
        var executor = new AllowlistedAiEditExecutor(session);
        var result = await executor.ExecutePlanAsync(new AiEditPlan
        {
            Status = "valid",
            Operation = "parameter.delta",
            Args = { ["key"] = key, ["delta"] = "0.4" }
        });

        Assert.True(result.Ok, result.Message);
        Assert.Equal(0.4f, session.ActiveCharacter!.ParametricHumanState!.LocalShapeParameters[key], 3);
    }

    [Fact]
    public async Task ParameterDelta_RefusesUnknownMorph()
    {
        var session = new ActiveProjectSession();
        var executor = new AllowlistedAiEditExecutor(session);
        var result = await executor.ExecutePlanAsync(new AiEditPlan
        {
            Status = "valid",
            Operation = "parameter.delta",
            Args = { ["key"] = "invented_magic_morph", ["delta"] = "0.5" }
        });

        Assert.False(result.Ok);
        Assert.Equal("Unsupported", result.Status);
        Assert.False(session.ActiveCharacter!.ParametricHumanState!.LocalShapeParameters.ContainsKey("invented_magic_morph"));
    }

    [Fact]
    public async Task OrcGreenMaterial_UsesExistingProjectMaterialPath()
    {
        var session = new ActiveProjectSession();
        var executor = new AllowlistedAiEditExecutor(session);
        var result = await executor.ExecutePlanAsync(new AiEditPlan
        {
            Status = "valid",
            Operation = "material.recolor",
            Args = { ["color"] = "orc green" }
        });

        Assert.True(result.Ok, result.Message);
        Assert.Single(session.Bundle.Materials);
        var color = session.Bundle.Materials[0].BaseColorFactor;
        Assert.Equal(0.28f, color.R, 2);
        Assert.Equal(0.42f, color.G, 2);
        Assert.Equal(0.22f, color.B, 2);
    }
}
