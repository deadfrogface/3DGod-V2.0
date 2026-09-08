using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure;

namespace ThreeDGodCreator.Core.Tests;

public class FeatureAvailabilityTests
{
    private readonly FeatureAvailabilityService _svc = new();

    [Theory]
    [InlineData(FeatureIds.AiGeneratePerson)]
    [InlineData(FeatureIds.AiGenerateAsset)]
    [InlineData(FeatureIds.RigAuto)]
    [InlineData(FeatureIds.ExportMetahuman)]
    [InlineData(FeatureIds.PhysicsSimulate)]
    [InlineData(FeatureIds.ClothingFit)]
    [InlineData(FeatureIds.ControllerInput)]
    [InlineData(FeatureIds.ExportUnreal)]
    public void PlaceholderFeatures_AreNotImplementedAndNotInvocable(string id)
    {
        Assert.Equal(FeatureAvailability.NotImplemented, _svc.GetStatus(id));
        Assert.False(_svc.IsInvocable(id));
        Assert.False(string.IsNullOrWhiteSpace(_svc.GetStatusMessage(id)));
        Assert.DoesNotContain("success", _svc.GetStatusMessage(id), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RealBaselineFeatures_AreAvailable()
    {
        Assert.Equal(FeatureAvailability.Available, _svc.GetStatus(FeatureIds.PresetSave));
        Assert.True(_svc.IsInvocable(FeatureIds.PresetSave));
        Assert.True(_svc.IsInvocable(FeatureIds.ViewportGlb));
        Assert.Equal(FeatureAvailability.Experimental, _svc.GetStatus(FeatureIds.ExportFbx));
        Assert.True(_svc.IsInvocable(FeatureIds.ExportFbx));
    }

    [Fact]
    public void DynamicService_ReportsAnnyFromRuntimeProbe()
    {
        var svc = new DynamicFeatureAvailabilityService();
        var status = svc.GetStatus(FeatureIds.AnnyHuman);
        Assert.DoesNotContain("success", svc.GetStatusMessage(FeatureIds.AnnyHuman), StringComparison.OrdinalIgnoreCase);
        Assert.True(status is FeatureAvailability.Experimental or FeatureAvailability.NotInstalled);
        Assert.Equal(FeatureAvailability.Available, svc.GetStatus(FeatureIds.ExportGlb));
        Assert.Equal(FeatureAvailability.Available, svc.GetStatus(FeatureIds.ProjectSave));
    }

    [Fact]
    public void Di_RegistersFeatureAvailabilityService()
    {
        using var provider = new ServiceCollection()
            .AddThreeDGodCoreServices()
            .BuildServiceProvider();
        var svc = provider.GetRequiredService<IFeatureAvailabilityService>();
        Assert.IsType<DynamicFeatureAvailabilityService>(svc);
    }
}
