using ThreeDGod.Infrastructure;
using Xunit;

namespace ThreeDGodCreator.Core.Tests;

public sealed class Cc0CreatureMorphTests
{
    [Fact]
    public void AnimalTargets_AreDiscoveredWithoutInventingNames()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-cc0-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var pack = Path.Combine(root, Cc0CreatureAssetPackService.Animal01.Id, "targets");
            Directory.CreateDirectory(pack);
            var cat = Path.Combine(pack, "titleknown_catgirl_ears.target");
            File.WriteAllText(cat, "# basemesh hm08\n42 0.1 0 0\n");

            var service = new Cc0CreatureAssetPackService(root: root);
            var targets = service.ListAnimalTargets();

            Assert.Single(targets);
            Assert.Equal(cat, service.FindAnimalTarget("catgirl", "ears"));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void Hm08TargetPack_IsRejectedForAnnyNativeTopology()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Cc0CreatureAssetPackService.AssertTargetCompatibleWithTopology(
                Cc0CreatureAssetPackService.Animal01, "anny"));

        Assert.Contains("Retarget/bake", ex.Message);
        Cc0CreatureAssetPackService.AssertTargetCompatibleWithTopology(
            Cc0CreatureAssetPackService.Animal01, "hm08");
    }

    [Fact]
    public void WorkerSource_RequiresMakeHumanTopologyForExternalTargets()
    {
        var root = FindRepoRoot();
        var worker = File.ReadAllText(Path.Combine(root, "workers", "anny", "anny_worker.py"));
        Assert.Contains("topology != \"makehuman\"", worker);
        Assert.Contains("_apply_makehuman_targets", worker);
        Assert.Contains("TargetTopologyMismatch", worker);
        Assert.DoesNotContain("topology = \"smplx\"", worker);
        Assert.Contains("topology not in (\"anny\", \"makehuman\")", worker);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "3DGodCreator.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
