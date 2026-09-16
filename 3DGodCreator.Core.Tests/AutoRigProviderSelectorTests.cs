using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure.AutoRig;

namespace ThreeDGodCreator.Core.Tests;

public class AutoRigProviderSelectorTests
{
    private sealed class FakeProvider : IAutoRigProvider
    {
        public FakeProvider(string id, AutoRigProviderKind kind, FeatureAvailability availability, string message = "")
        {
            ProviderId = id;
            Kind = kind;
            Availability = availability;
            Message = message;
        }

        public string ProviderId { get; }
        public AutoRigProviderKind Kind { get; }
        public string DisplayName => ProviderId;
        public FeatureAvailability Availability { get; }
        public string Message { get; }
        public int RigCalls { get; private set; }

        public AutoRigProviderStatus Probe() => new()
        {
            ProviderId = ProviderId,
            Kind = Kind,
            Availability = Availability,
            Message = Message,
            DisplayName = DisplayName
        };

        public Task<AutoRigResult> RigAsync(string sourceGlb, string destinationGlb, CancellationToken cancellationToken = default)
        {
            RigCalls++;
            File.WriteAllText(destinationGlb, "not-a-real-glb");
            return Task.FromResult(new AutoRigResult
            {
                OutputGlb = destinationGlb,
                ProviderId = ProviderId,
                Kind = Kind,
                Device = Kind.ToString(),
                Provenance = "fake-for-selection-logic-only"
            });
        }
    }

    [Fact]
    public void Automatic_PrefersVulkan_ThenCpu_ThenCuda()
    {
        var vulkan = new FakeProvider("vk", AutoRigProviderKind.SkinTokensCppVulkan, FeatureAvailability.Experimental);
        var cpu = new FakeProvider("cpu", AutoRigProviderKind.SkinTokensCppCpu, FeatureAvailability.Experimental);
        var cuda = new FakeProvider("cuda", AutoRigProviderKind.SkinTokensOfficialCuda, FeatureAvailability.Experimental);
        var sel = new AutoRigProviderSelector([cpu, vulkan, cuda]).Select(AutoRigDevicePreference.Automatic);
        Assert.Equal("vk", sel.Selected!.ProviderId);
    }

    [Fact]
    public void Automatic_FallsBackToCpu_WhenVulkanUnavailable()
    {
        var vulkan = new FakeProvider("vk", AutoRigProviderKind.SkinTokensCppVulkan, FeatureAvailability.UnsupportedHardware, "no vulkan");
        var cpu = new FakeProvider("cpu", AutoRigProviderKind.SkinTokensCppCpu, FeatureAvailability.Experimental);
        var sel = new AutoRigProviderSelector([vulkan, cpu]).Select(AutoRigDevicePreference.Automatic);
        Assert.Equal("cpu", sel.Selected!.ProviderId);
        Assert.Contains(sel.Unavailable, u => u.ProviderId == "vk");
    }

    [Fact]
    public void Automatic_UsesCuda_WhenOnlyCudaAvailable()
    {
        var cuda = new FakeProvider("cuda", AutoRigProviderKind.SkinTokensOfficialCuda, FeatureAvailability.Experimental);
        var cpu = new FakeProvider("cpu", AutoRigProviderKind.SkinTokensCppCpu, FeatureAvailability.NotInstalled);
        var sel = new AutoRigProviderSelector([cpu, cuda]).Select(AutoRigDevicePreference.Automatic);
        Assert.Equal("cuda", sel.Selected!.ProviderId);
    }

    [Fact]
    public async Task VulkanPreference_DoesNotSilentlyRunCpu()
    {
        var cpu = new FakeProvider("cpu", AutoRigProviderKind.SkinTokensCppCpu, FeatureAvailability.Experimental);
        var vulkan = new FakeProvider("vk", AutoRigProviderKind.SkinTokensCppVulkan, FeatureAvailability.UnsupportedHardware);
        var selector = new AutoRigProviderSelector([cpu, vulkan]);
        var dest = Path.Combine(Path.GetTempPath(), "autoroot-vk-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                selector.RigAsync("in.glb", dest, AutoRigDevicePreference.Vulkan));
            Assert.Contains("Vulkan", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, cpu.RigCalls);
        }
        finally
        {
            if (File.Exists(dest)) File.Delete(dest);
        }
    }

    [Fact]
    public void Composition_ResolvesAutoRigService()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        ThreeDGod.Infrastructure.ThreeDGodComposition.AddThreeDGodCoreServices(services);
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IAutoRigService>());
        Assert.NotNull(sp.GetRequiredService<IAutoRigProviderSelector>());
        Assert.NotEmpty(sp.GetServices<IAutoRigProvider>());
    }
}
