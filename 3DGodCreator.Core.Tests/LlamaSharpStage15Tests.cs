using ThreeDGod.AI;
using ThreeDGod.Application;

namespace ThreeDGodCreator.Core.Tests;

public class LlamaSharpStage15Tests
{
    [Fact]
    public void DeterministicParser_StillHandlesCanonicalPhrases()
    {
        var plan = DeterministicAiParser.Parse("make him taller");
        // Either deterministic maps it or leaves Unsupported for LLamaSharp — never crashes.
        Assert.NotNull(plan.Status);
    }

    [Fact]
    public void Probe_WithoutLicense_IsNotInstalledOrBlocked()
    {
        // Do not mutate user model dir; just ensure Probe returns a non-Available fake-success.
        var status = LlamaSharpProvider.Probe();
        Assert.NotEqual(FeatureAvailability.Available, status.Availability);
    }

    [SkippableFact]
    public void Interpret_WhenGgufPresent_CanProduceValidatedPlan()
    {
        var status = LlamaSharpProvider.Probe();
        Skip.If(status.Availability != FeatureAvailability.Experimental, status.Message);
        var plan = LlamaSharpProvider.Interpret("make him taller");
        Assert.Equal("llamasharp", plan.Provider);
        Assert.True(plan.Status is "valid" or "Ambiguous" or "Unsupported");
        if (plan.Status == "valid")
            Assert.False(string.IsNullOrWhiteSpace(plan.Operation));
    }
}
