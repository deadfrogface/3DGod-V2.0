using System.Xml.Linq;
using ThreeDGod.Infrastructure.Components;

namespace ThreeDGodCreator.Core.Tests;

public class Stage12Through19HonestyTests
{
    [Fact]
    public void SetupAssistant_GatedProviders_AreNotInstallable_FromRepo()
    {
        var repo = RepoPaths.FindRepoRoot();
        var root = Path.Combine(Path.GetTempPath(), "3dgod-setup-gates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mgr = ComponentManager.FromRepo(repo, root);
            var snap = SetupAssistantCatalog.Snapshot(mgr);

            var image = Assert.Single(snap, s => s.Feature.FeatureId == SetupFeatureId.ImageTo3D);
            Assert.False(image.CanInstall);
            Assert.Equal("triposr", image.Feature.PrimaryComponentId);

            var flux = Assert.Single(snap, s => s.Feature.FeatureId == SetupFeatureId.TextToCharacter);
            Assert.False(flux.CanInstall);

            var advanced = Assert.Single(snap, s => s.Feature.FeatureId == SetupFeatureId.Advanced3DQuality);
            Assert.False(advanced.CanInstall);
            Assert.Contains("REJECTED", advanced.Message, StringComparison.OrdinalIgnoreCase);

            var skin = Assert.Single(snap, s => s.Feature.FeatureId == SetupFeatureId.AutomaticRigging);
            Assert.False(skin.CanInstall);
            Assert.Contains("GATED", skin.Message, StringComparison.OrdinalIgnoreCase);

            var human = Assert.Single(snap, s => s.Feature.FeatureId == SetupFeatureId.HumanCreator);
            var anny = Assert.Single(mgr.ListManifests(), m => m.ComponentId == "anny");
            Assert.False(string.IsNullOrWhiteSpace(anny.LocalSourceHint));
            Assert.True(human.CanInstall);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void CanOfferInstall_RequiresSource_AndNeverDownloadUnavailable()
    {
        Assert.False(SetupAssistantCatalog.CanOfferInstall(
            new ComponentManifest { ComponentId = "triposr" },
            ComponentState.DownloadUnavailable));

        Assert.False(SetupAssistantCatalog.CanOfferInstall(
            new ComponentManifest { ComponentId = "triposr", Optional = true },
            ComponentState.Optional));

        Assert.True(SetupAssistantCatalog.CanOfferInstall(
            new ComponentManifest { ComponentId = "anny", LocalSourceHint = "workers/anny" },
            ComponentState.Optional));

        Assert.False(SetupAssistantCatalog.CanOfferInstall(
            new ComponentManifest { ComponentId = "sf3d", DownloadUrl = "https://example.invalid/sf3d.zip" },
            ComponentState.NotInstalled));
    }

    [Fact]
    public void Stage17_NoNativeMeshoptimizerOrXatlasOrFlaUiPackageReferences()
    {
        var repo = RepoPaths.FindRepoRoot();
        var forbidden = new[] { "meshoptimizer", "xatlas", "xatlas.NET", "FlaUI" };
        foreach (var csproj in Directory.EnumerateFiles(repo, "*.csproj", SearchOption.AllDirectories))
        {
            if (csproj.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                csproj.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            var xml = XDocument.Load(csproj);
            var packages = xml.Descendants("PackageReference")
                .Select(e => (string?)e.Attribute("Include") ?? "")
                .Where(s => s.Length > 0)
                .ToList();
            foreach (var bad in forbidden)
                Assert.DoesNotContain(packages, p => string.Equals(p, bad, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Stage19_FlaUiRemainsGated_InstalledAppSmokeExists()
    {
        var repo = RepoPaths.FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(repo, "docs", "audit", "STAGE19_FLAUI_DECISION.md")));
        Assert.True(File.Exists(Path.Combine(repo, "3DGodCreator.App", "InstalledAppSmoke.cs")));
        Assert.Contains(
            "GATED_EXTERNAL_RUNNER",
            File.ReadAllText(Path.Combine(repo, "docs", "audit", "STAGE12_19_GATES.md")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Stage13_16_17_DecisionDocsExist()
    {
        var repo = RepoPaths.FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(repo, "docs", "audit", "STAGE13_SF3D_SPAR3D_DECISION.md")));
        Assert.True(File.Exists(Path.Combine(repo, "docs", "audit", "STAGE16_SKINTOKENS_DECISION.md")));
        Assert.True(File.Exists(Path.Combine(repo, "docs", "audit", "STAGE17_MESHOPT_XATLAS_DECISION.md")));
    }
}
