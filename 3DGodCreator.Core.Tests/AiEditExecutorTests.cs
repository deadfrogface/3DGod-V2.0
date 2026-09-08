using ThreeDGod.AI;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;

namespace ThreeDGodCreator.Core.Tests;

public class AiEditExecutorTests
{
    [Fact]
    public async Task GroesserUndHautDunkler_IsTwoEdits_OneUndo()
    {
        var human = new ParametricHumanState { PhenotypeParameters = { ["height"] = 0.5f } };
        var material = new MaterialDefinition { BaseColorFactor = new ColorRgba { R = 0.8f, G = 0.6f, B = 0.5f, A = 1f } };
        var target = new AiEditTarget { Human = human, Material = material };
        var stack = new CommandStack();
        var plans = DeterministicAiParser.ParseComposite("größer und haut dunkler");
        Assert.Equal(2, plans.Count);
        Assert.All(plans, p => Assert.Equal("valid", p.Status));

        await AiEditExecutor.ExecuteAsync(plans, target, stack);
        Assert.True(human.PhenotypeParameters["height"] > 0.5f);
        Assert.True(material.BaseColorFactor.R < 0.8);
        Assert.Equal(1, stack.UndoCount);

        await stack.UndoAsync();
        Assert.Equal(0.5f, human.PhenotypeParameters["height"]);
        Assert.Equal(0.8f, material.BaseColorFactor.R, 3);
        Assert.True(stack.CanRedo);
    }

    [Fact]
    public async Task AttachmentAddRemove_MutatesCollection()
    {
        var target = new AiEditTarget();
        var stack = new CommandStack();
        await AiEditExecutor.ExecuteAsync(
            [new AiEditPlan { Status = "valid", Operation = "attachment.add", Args = { ["type"] = "necklace" } }],
            target,
            stack);
        Assert.Contains("necklace", target.Attachments);
        await AiEditExecutor.ExecuteAsync(
            [new AiEditPlan { Status = "valid", Operation = "attachment.remove", Args = { ["type"] = "necklace" } }],
            target,
            stack);
        Assert.DoesNotContain("necklace", target.Attachments);
    }
}
