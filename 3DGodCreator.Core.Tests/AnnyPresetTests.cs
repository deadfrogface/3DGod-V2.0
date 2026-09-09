using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

[Collection("AnnySerial")]
public class AnnyPresetTests
{
    [Fact]
    public void BuiltInPresets_AreDataNotThumbnails()
    {
        var store = new AnnyPresetStore(RepoPaths.FindRepoRoot());
        var presets = store.List();
        Assert.Contains(presets, p => p.Name == "adult-average" && p.BuiltIn);
        Assert.Contains(presets, p => p.Name == "muscular-male" && p.BuiltIn);
        Assert.Contains(presets, p => p.Name == "tall-slim" && p.BuiltIn);
        foreach (var preset in presets.Where(p => p.BuiltIn))
        {
            Assert.Equal("anny", preset.Backend);
            Assert.True(preset.Phenotypes.Count >= 6);
            Assert.True(string.IsNullOrWhiteSpace(preset.ThumbnailPath));
        }
    }

    [Fact]
    public void SaveUserPreset_Reload_ReproducesParams()
    {
        var store = new AnnyPresetStore(RepoPaths.FindRepoRoot());
        var name = "user-" + Guid.NewGuid().ToString("N")[..8];
        var path = store.SaveUser(new AnnyHumanPreset
        {
            Name = name,
            Backend = "anny",
            Phenotypes = { ["muscle"] = 0.77f, ["height"] = 0.61f },
            Tags = ["user", "test"]
        });
        try
        {
            var loaded = store.Load(name);
            Assert.False(loaded.BuiltIn);
            Assert.Equal(0.77f, loaded.Phenotypes["muscle"]);
            Assert.Equal(0.61f, loaded.Phenotypes["height"]);
            Assert.Contains("user", loaded.Tags);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [SkippableFact]
    public async Task PresetApply_ChangesMesh_UndoRestoresParams()
    {
        var probe = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("Anny uv/runtime missing; preset mesh apply test not executed.");
            return;
        }

        var store = new AnnyPresetStore(RepoPaths.FindRepoRoot());
        var average = store.Load("adult-average");
        var muscle = store.Load("muscular-male");
        await using var svc = new AnnyHumanService(new WorkerProcessHost());
        var aPath = Path.Combine(Path.GetTempPath(), "3dgod-preset-a-" + Guid.NewGuid().ToString("N") + ".glb");
        var bPath = Path.Combine(Path.GetTempPath(), "3dgod-preset-b-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            await svc.GenerateGlbAsync(aPath, new AnnyGenerateRequest { Phenotypes = average.Phenotypes });
            await svc.GenerateGlbAsync(bPath, new AnnyGenerateRequest { Phenotypes = muscle.Phenotypes });
            var a = MeshCompare.ReadPositions(aPath);
            var b = MeshCompare.ReadPositions(bPath);
            Assert.True(a.Count > 1000);
            Assert.Equal(a.Count, b.Count);
            var moved = a.Zip(b, (l, r) => System.Numerics.Vector3.Distance(l, r) > 0.002f).Count(x => x);
            Assert.True(moved > 100, $"Preset must change vertices, moved={moved}");
            Assert.False(MeshCompare.IsUniformScale(a, b));
        }
        finally
        {
            if (File.Exists(aPath)) File.Delete(aPath);
            if (File.Exists(bPath)) File.Delete(bPath);
        }

        var state = AnnyPresetMapper.ToState(average);
        var stack = new CommandStack();
        await stack.ExecuteAsync(new PropertyChangeCommand(
            Guid.NewGuid(),
            "anny.preset",
            AnnyInspectorClone(state),
            AnnyPresetMapper.ToState(muscle),
            value => CopyInto(state, (ParametricHumanState)value!)));
        Assert.Equal(0.9f, state.PhenotypeParameters["muscle"]);
        await stack.UndoAsync();
        Assert.Equal(0.45f, state.PhenotypeParameters["muscle"]);
    }

    private static ParametricHumanState AnnyInspectorClone(ParametricHumanState source) =>
        new()
        {
            BackendId = source.BackendId,
            PhenotypeParameters = new Dictionary<string, float>(source.PhenotypeParameters),
            LocalShapeParameters = new Dictionary<string, float>(source.LocalShapeParameters),
            FacialActionParameters = new Dictionary<string, float>(source.FacialActionParameters),
            SourcePreset = source.SourcePreset
        };

    private static void CopyInto(ParametricHumanState target, ParametricHumanState source)
    {
        target.PhenotypeParameters.Clear();
        foreach (var kv in source.PhenotypeParameters)
            target.PhenotypeParameters[kv.Key] = kv.Value;
        target.SourcePreset = source.SourcePreset;
    }
}
