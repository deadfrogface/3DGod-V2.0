using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

[Collection("AnnySerial")]
public class AnnyLiveTests
{
    [SkippableFact]
    public async Task Catalog_HasAtLeastTenLiveParameters()
    {
        var probe = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("Anny uv/runtime missing; live catalog probe not executed.");
            return;
        }
        await using var svc = new AnnyHumanService(new WorkerProcessHost());
        var catalog = await svc.GetCatalogAsync();
        Assert.True(catalog.Count >= 10, $"Catalog has {catalog.Count} keys.");
        Assert.True(catalog.PhenotypeKeys.Count >= 6);
        Assert.NotEmpty(catalog.LocalChangeKeys);
        Assert.NotEmpty(catalog.FacialActionKeys);
    }

    [SkippableFact]
    public async Task TenParameters_ChangeVertices_AndAreNotUniformScale()
    {
        var probe = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("Anny uv/runtime missing; live parameter vertex test not executed.");
            return;
        }

        await using var svc = new AnnyHumanService(new WorkerProcessHost());
        var catalog = await svc.GetCatalogAsync();
        var keys = catalog.PhenotypeKeys.Take(8).Concat(catalog.LocalChangeKeys.Take(2)).ToArray();
        Assert.True(keys.Length >= 10, "Need 10 catalog keys.");

        var low = new Dictionary<string, float>();
        var high = new Dictionary<string, float>();
        var localLow = new Dictionary<string, float>();
        var localHigh = new Dictionary<string, float>();
        foreach (var key in catalog.PhenotypeKeys.Take(8))
        {
            low[key] = 0.15f;
            high[key] = 0.85f;
        }
        foreach (var key in catalog.LocalChangeKeys.Take(2))
        {
            localLow[key] = -0.8f;
            localHigh[key] = 0.8f;
        }

        var aPath = Path.Combine(Path.GetTempPath(), "3dgod-anny-a-" + Guid.NewGuid().ToString("N") + ".glb");
        var bPath = Path.Combine(Path.GetTempPath(), "3dgod-anny-b-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            await svc.GenerateGlbAsync(aPath, new AnnyGenerateRequest { Phenotypes = low, LocalChanges = localLow });
            await svc.GenerateGlbAsync(bPath, new AnnyGenerateRequest { Phenotypes = high, LocalChanges = localHigh });
            var a = MeshCompare.ReadPositions(aPath);
            var b = MeshCompare.ReadPositions(bPath);
            Assert.True(a.Count > 1000);
            Assert.Equal(a.Count, b.Count);
            var moved = 0;
            for (var i = 0; i < a.Count; i++)
            {
                if (System.Numerics.Vector3.Distance(a[i], b[i]) > 0.002f)
                    moved++;
            }
            Assert.True(moved > 100, $"Expected visible vertex change, moved={moved}");
            Assert.False(MeshCompare.IsUniformScale(a, b), "Parameter edits must not be a uniform scale morph.");
        }
        finally
        {
            if (File.Exists(aPath)) File.Delete(aPath);
            if (File.Exists(bPath)) File.Delete(bPath);
        }
    }

    [Fact]
    public async Task ParameterChange_UndoRedo_RestoresValues()
    {
        var state = new ParametricHumanState { PhenotypeParameters = { ["muscle"] = 0.2f } };
        var stack = new CommandStack();
        await stack.ExecuteAsync(new PropertyChangeCommand(
            Guid.NewGuid(),
            "body:muscle",
            0.2f,
            0.9f,
            value => state.PhenotypeParameters["muscle"] = Convert.ToSingle(value)));
        Assert.Equal(0.9f, state.PhenotypeParameters["muscle"]);
        await stack.UndoAsync();
        Assert.Equal(0.2f, state.PhenotypeParameters["muscle"]);
        await stack.RedoAsync();
        Assert.Equal(0.9f, state.PhenotypeParameters["muscle"]);
    }

    [Fact]
    public async Task ProjectSaveReload_ReproducesAnnyState()
    {
        var archive = new GodProjectArchive();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".3dgod");
        try
        {
            var character = new CharacterDocument
            {
                Name = "AnnyLive",
                SourceRepresentation = SourceRepresentation.AnnyParameters,
                ParametricHumanState = new ParametricHumanState
                {
                    BackendId = "anny",
                    TopologyProfile = "anny",
                    PhenotypeParameters = { ["gender"] = 0.7f, ["muscle"] = 0.4f, ["height"] = 0.55f },
                    LocalShapeParameters = { ["l-upperarm-fat-incr"] = 0.3f },
                    FacialActionParameters = { ["jawOpen"] = 0.2f }
                }
            };
            var bundle = new ProjectBundle
            {
                Project = new ProjectDocument { Name = "AnnyLive", CharacterIds = [character.CharacterId] },
                Characters = [character]
            };
            await archive.SaveAsync(bundle, path);
            var loaded = await archive.LoadAsync(path);
            var state = loaded.Characters.Single().ParametricHumanState;
            Assert.NotNull(state);
            Assert.Equal("anny", state!.BackendId);
            Assert.Equal(0.7f, state.PhenotypeParameters["gender"]);
            Assert.Equal(0.4f, state.PhenotypeParameters["muscle"]);
            Assert.Equal(0.55f, state.PhenotypeParameters["height"]);
            Assert.Equal(0.3f, state.LocalShapeParameters["l-upperarm-fat-incr"]);
            Assert.Equal(0.2f, state.FacialActionParameters["jawOpen"]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }
    }
}
