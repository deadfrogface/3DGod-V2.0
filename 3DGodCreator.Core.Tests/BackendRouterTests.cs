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
}
