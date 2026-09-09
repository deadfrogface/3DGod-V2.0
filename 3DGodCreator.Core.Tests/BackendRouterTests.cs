using ThreeDGod.Application;

namespace ThreeDGodCreator.Core.Tests;

public class BackendRouterTests
{
    [Fact]
    public void Router_ExcludesInsufficientVramLicenseAndUnavailable_ThenSelectsFallback()
    {
        var heavy = new BackendManifest
        {
            Id = "anny",
            Priority = 100,
            State = BackendRuntimeState.Available,
            Hardware = new HardwareRequirement { MinVramMb = 24000, RequiresCuda = true },
            License = new LicenseProfile { Id = "anny", Accepted = true },
            Capabilities = new BackendCapabilities { HumanGenerate = true }
        };
        var blocked = new BackendManifest
        {
            Id = "blocked",
            Priority = 90,
            State = BackendRuntimeState.LicenseBlocked,
            License = new LicenseProfile { Id = "x", Accepted = false },
            Capabilities = new BackendCapabilities { HumanGenerate = true }
        };
        var down = new BackendManifest
        {
            Id = "down",
            Priority = 80,
            State = BackendRuntimeState.Unavailable,
            License = new LicenseProfile { Id = "d", Accepted = true },
            Capabilities = new BackendCapabilities { HumanGenerate = true }
        };
        var cpu = new BackendManifest
        {
            Id = "echo",
            Priority = 10,
            State = BackendRuntimeState.Available,
            Hardware = new HardwareRequirement { MinVramMb = 0 },
            License = new LicenseProfile { Id = "mit", Accepted = true },
            Capabilities = new BackendCapabilities { HumanGenerate = true }
        };

        var router = new BackendRouter();
        var result = router.Route([heavy, blocked, down, cpu], availableVramMb: 4096, cudaAvailable: false, capability: "human.generate");
        Assert.NotNull(result.Provider);
        Assert.Equal("echo", result.Provider!.Id);
        Assert.Equal("selected", result.Reason);
    }

    [Fact]
    public void ImageTo3D_Router_ExcludesLicenseVramAndOptionalTrellis()
    {
        var sf3d = new BackendManifest
        {
            Id = "sf3d",
            Priority = 70,
            State = BackendRuntimeState.LicenseBlocked,
            License = new LicenseProfile { Id = "stability-community", Accepted = false },
            Hardware = new HardwareRequirement { MinVramMb = 8192, RequiresCuda = true },
            Capabilities = new BackendCapabilities { ImageTo3d = true }
        };
        var spar = new BackendManifest
        {
            Id = "spar3d",
            Priority = 60,
            State = BackendRuntimeState.Available,
            License = new LicenseProfile { Id = "stability-community", Accepted = true },
            Hardware = new HardwareRequirement { MinVramMb = 6144, RequiresCuda = true },
            Capabilities = new BackendCapabilities { ImageTo3d = true }
        };
        var trellis = new BackendManifest
        {
            Id = "trellis",
            Priority = 20,
            State = BackendRuntimeState.NotInstalled,
            License = new LicenseProfile { Id = "trellis", Accepted = false },
            Hardware = new HardwareRequirement { MinVramMb = 12288, RequiresCuda = true },
            Capabilities = new BackendCapabilities { ImageTo3d = true }
        };

        var router = new BackendRouter();
        var noHw = router.Route([sf3d, spar, trellis], availableVramMb: 4096, cudaAvailable: false, capability: "image.to3d");
        Assert.Null(noHw.Provider);

        var licensed = router.Route([sf3d, spar, trellis], availableVramMb: 8192, cudaAvailable: true, capability: "image.to3d");
        Assert.Equal("spar3d", licensed.Provider?.Id);
    }

    [Fact]
    public void Spar3dProfiles_ExposeNormalAndLowVram()
    {
        var profiles = ThreeDGod.Infrastructure.ImageTo3DProfiles.For("spar3d");
        Assert.Contains(profiles, p => p.Name == "normal" && p.MinVramMb == 12288);
        Assert.Contains(profiles, p => p.Name == "low-vram" && p.MinVramMb == 6144);
        Assert.Null(ThreeDGod.Infrastructure.ImageTo3DProfiles.Select("spar3d", availableVramMb: 2048, cuda: true));
        Assert.Equal("low-vram", ThreeDGod.Infrastructure.ImageTo3DProfiles.Select("spar3d", 7000, true)?.Name);
    }

    [Fact]
    public void Sf3d_WithoutStabilityLicense_IsBlocked()
    {
        var status = ThreeDGod.Infrastructure.ImageTo3DRuntime.Probe("sf3d");
        Assert.Equal(ThreeDGod.Application.FeatureAvailability.Disabled, status.Availability);
        Assert.Contains("LicenseBlocked", status.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Trellis_IsOptionalAndNotInstalled()
    {
        var status = ThreeDGod.Infrastructure.ImageTo3DRuntime.Probe("trellis");
        Assert.NotEqual(ThreeDGod.Application.FeatureAvailability.Available, status.Availability);
        Assert.DoesNotContain("success", status.Message, StringComparison.OrdinalIgnoreCase);
    }
}
