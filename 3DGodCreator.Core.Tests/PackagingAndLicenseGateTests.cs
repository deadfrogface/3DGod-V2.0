using ThreeDGod.Infrastructure;

namespace ThreeDGodCreator.Core.Tests;

public class PackagingAndLicenseGateTests
{
    [Fact]
    public void ModelLicensesJson_ParsesAndHasProductionComponents()
    {
        var registry = ReleaseLicenseGate.LoadRegistry(RepoPaths.FindRepoRoot());
        Assert.Equal(1, registry.SchemaVersion);
        Assert.NotEmpty(registry.Components);
        Assert.Contains(registry.Components, c => c.Id == "3dgod-app" && c.ShippingStatus == "production");
        Assert.Contains(registry.Blocklist.ModelIds, id => id == "smpl-x");
    }

    [Fact]
    public void ReleaseLicenseGate_ValidatesRegistryAndManifests()
    {
        var result = ReleaseLicenseGate.ValidateForRelease(RepoPaths.FindRepoRoot());
        Assert.True(result.Ok, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("smpl-x")]
    [InlineData("hunyuan-eu")]
    [InlineData("anigen")]
    public void Blocklist_RejectsKnownBadModelIds(string id)
    {
        Assert.True(ReleaseLicenseGate.IsBlockedModelId(id));
        var gate = new LicenseGate();
        Assert.False(gate.TryAcceptModel(id, "apache-2.0", userAccepted: true));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("s-lab-nc")]
    [InlineData("gpl-3.0-linked")]
    [InlineData("smpl-x-noncommercial")]
    public void Blocklist_RejectsKnownBadLicenseIds(string licenseId)
    {
        Assert.True(ReleaseLicenseGate.IsBlockedLicenseId(licenseId));
        var gate = new LicenseGate();
        Assert.False(gate.TryAccept(licenseId, userAccepted: true));
    }

    [Fact]
    public void LicenseGate_RequiresExplicitAccept_ForAllowedProfile()
    {
        var gate = new LicenseGate();
        Assert.False(gate.TryAccept("apache-2.0", false));
        Assert.True(gate.TryAccept("apache-2.0", true));
        Assert.True(gate.TryAccept("stability-community", true));
    }

    [Fact]
    public void WorkerManifests_MatchSchemaAndExcludeModelsFromBaseInstaller()
    {
        var root = RepoPaths.FindRepoRoot();
        var paths = WorkerPackageManifestReader.DiscoverManifestPaths(root);
        Assert.True(paths.Count >= 10, "Expected worker manifests under workers/ and docs/packaging/workers/");
        foreach (var path in paths)
        {
            var manifest = WorkerPackageManifestReader.Load(path);
            var validation = WorkerPackageManifestReader.Validate(manifest, root);
            Assert.True(validation.Ok, $"{path}: {string.Join("; ", validation.Errors)}");
            Assert.False(manifest.IncludedInBaseInstaller);
        }
    }

    [Fact]
    public void AnnyAndGarmentManifests_AreExperimentalWithUvLock()
    {
        var root = RepoPaths.FindRepoRoot();
        var anny = WorkerPackageManifestReader.Load(Path.Combine(root, "workers", "anny", "manifest.json"));
        var garment = WorkerPackageManifestReader.Load(Path.Combine(root, "workers", "garmentcode", "manifest.json"));
        Assert.Equal("Experimental", anny.Status);
        Assert.Equal("Experimental", garment.Status);
        Assert.True(File.Exists(Path.Combine(root, "workers", "anny", "uv.lock")));
        Assert.True(File.Exists(Path.Combine(root, "workers", "garmentcode", "uv.lock")));
    }

    [Fact]
    public void VelopackUpdateChannel_ReadsDefaults()
    {
        var prev = Environment.GetEnvironmentVariable("THREEDGOD_UPDATE_CHANNEL");
        try
        {
            Environment.SetEnvironmentVariable("THREEDGOD_UPDATE_CHANNEL", null);
            var channel = VelopackUpdateChannel.FromEnvironment();
            Assert.Equal(VelopackUpdateChannel.DefaultChannel, channel.Channel);
            Assert.Equal(VelopackUpdateChannel.PackId, VelopackUpdateChannel.PackId);
        }
        finally
        {
            Environment.SetEnvironmentVariable("THREEDGOD_UPDATE_CHANNEL", prev);
        }
    }

    [Fact]
    public void ThirdPartyNotices_ExistsAtRepoRoot()
    {
        Assert.True(File.Exists(Path.Combine(RepoPaths.FindRepoRoot(), "THIRD_PARTY_NOTICES.txt")));
    }
}
