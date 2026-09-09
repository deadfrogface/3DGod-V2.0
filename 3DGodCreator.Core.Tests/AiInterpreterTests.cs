using ThreeDGod.AI;
using ThreeDGod.Application;

namespace ThreeDGodCreator.Core.Tests;

public class AiInterpreterTests
{
    [Theory]
    [InlineData("shoulders wider")]
    [InlineData("jacket red")]
    [InlineData("remove necklace")]
    [InlineData("taller, keep head size")]
    [InlineData("gold chain")]
    [InlineData("rat head")]
    [InlineData("Rattenkopf.")]
    [InlineData("Hörner.")]
    [InlineData("rechte Hand mechanisch.")]
    public void DeterministicCorpus_IsValidAllowListedPlan(string prompt)
    {
        var plan = new AiCommandInterpreter().Interpret(prompt);
        Assert.Equal("valid", plan.Status);
        Assert.NotNull(plan.Operation);
        Assert.Contains(plan.Operation!, AiEditPlanSchema.AllowedOperations);
        Assert.DoesNotContain("eval", DeterministicAiParser.ToJson(plan), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VaguePrompt_IsAmbiguous()
    {
        Assert.Equal("Ambiguous", new AiCommandInterpreter().Interpret("make it nicer").Status);
    }

    [Fact]
    public void UnknownPrompt_IsUnsupported_AndNeverCode()
    {
        var plan = new AiCommandInterpreter().Interpret("drop table characters");
        Assert.Equal("Unsupported", plan.Status);
        Assert.DoesNotContain("eval", DeterministicAiParser.ToJson(plan), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validator_RejectsUnknownOperation()
    {
        var plan = AiEditPlanValidator.Validate(new AiEditPlan
        {
            Status = "valid",
            Operation = "eval(os.system)",
            Args = { ["code"] = "print(1)" }
        });
        Assert.Equal("Unsupported", plan.Status);
    }

    [Fact]
    public void LlamaSharp_IsHonestWithoutGguf()
    {
        var status = LlamaSharpProvider.Probe();
        Assert.True(status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.Disabled or FeatureAvailability.Experimental);
        Assert.DoesNotContain("success", status.Message, StringComparison.OrdinalIgnoreCase);
        if (status.Availability != FeatureAvailability.Experimental)
        {
            var plan = LlamaSharpProvider.Interpret("shoulders wider");
            Assert.Equal("Unsupported", plan.Status);
            Assert.Contains("NotInstalled", plan.Reason + status.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
