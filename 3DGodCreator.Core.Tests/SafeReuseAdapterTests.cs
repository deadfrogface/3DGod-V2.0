using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.AI;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Rigging;

namespace ThreeDGodCreator.Core.Tests;

public class SafeReuseAdapterTests
{
    private static (List<Vector3> Positions, List<int> Indices) UnitGrid(int divisions = 6)
    {
        var positions = new List<Vector3>();
        var indices = new List<int>();
        for (var z = 0; z <= divisions; z++)
        for (var x = 0; x <= divisions; x++)
            positions.Add(new Vector3(x / (float)divisions, 0, z / (float)divisions));
        for (var z = 0; z < divisions; z++)
        for (var x = 0; x < divisions; x++)
        {
            var i = z * (divisions + 1) + x;
            indices.Add(i); indices.Add(i + 1); indices.Add(i + divisions + 1);
            indices.Add(i + 1); indices.Add(i + divisions + 2); indices.Add(i + divisions + 1);
        }
        return (positions, indices);
    }

    [Fact]
    public void VertexClusterProcessor_MatchesLegacyRemeshPipelineBackendId()
    {
        var (pos, idx) = UnitGrid();
        var legacy = RemeshPipeline.Run(pos, idx, RemeshProfile.Preview);
        var adapted = new VertexClusterMeshProcessor().Process(pos, idx, RemeshProfile.Preview);
        Assert.Equal("vertex-cluster", adapted.BackendId);
        Assert.Equal(legacy.Indices.Count / 3, adapted.Indices.Count / 3);
        Assert.False(MeshValidator.Validate(adapted.Positions, adapted.Indices, adapted.Uvs.Count).Rejected);
    }

    [Fact]
    public void Geometry3SharpQemProcessor_ProducesValidSmallerMesh_ButDefaultStaysCluster()
    {
        var (pos, idx) = UnitGrid(10);
        var cluster = MeshProcessorSelector.Create(MeshProcessorSelector.DefaultBackendId)
            .Process(pos, idx, RemeshProfile.StaticGameAsset);
        var qem = MeshProcessorSelector.Create(MeshProcessorSelector.Geometry3SharpBackendId)
            .Process(pos, idx, RemeshProfile.StaticGameAsset);

        Assert.Equal(MeshProcessorSelector.DefaultBackendId, cluster.BackendId);
        Assert.Equal(MeshProcessorSelector.Geometry3SharpBackendId, qem.BackendId);
        Assert.True(qem.Indices.Count / 3 < idx.Count / 3);
        Assert.False(MeshValidator.Validate(qem.Positions, qem.Indices, qem.Uvs.Count).Rejected);
        // Default selector remains cluster — no silent replace.
        Assert.Equal(MeshProcessorSelector.DefaultBackendId, MeshProcessorSelector.Create().BackendId);
    }

    [Fact]
    public void DistanceSkinWeights_AreNormalized_AndMultiBoneOnFreeformTorso()
    {
        var solver = new DistanceSkinWeightSolver();
        var weights = solver.Solve(new Vector3(0, 0.30f, 0.02f), FreeformCreatureRig.JointPositions, 4);
        Assert.InRange(weights.Sum(w => w.Weight), 0.99f, 1.01f);
        Assert.True(weights.Count >= 2);
    }

    [Fact]
    public void AttachmentSockets_ShareBinder_ForTailHornHair()
    {
        var characterId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var tail = AttachmentSockets.Attach(characterId, assetId, AttachmentType.Tail);
        var horn = AttachmentSockets.Attach(characterId, assetId, AttachmentType.Horn);
        var hair = AttachmentSockets.Attach(characterId, assetId, AttachmentType.Hair);
        Assert.Equal("hips", tail.ParentBoneSemantic);
        Assert.Equal("head", horn.ParentBoneSemantic);
        Assert.Equal("head", hair.ParentBoneSemantic);
        Assert.Equal(AttachmentDefaults.SlotFor(AttachmentType.Tail), tail.ParentBoneSemantic);
    }

    [Fact]
    public void DeterministicAi_LongerTail_AndSoftGold_AreValidPlans()
    {
        var tail = DeterministicAiParser.Parse("longer tail");
        Assert.Equal("valid", tail.Status);
        Assert.Equal("parameter.delta", tail.Operation);
        Assert.Equal("tailLength", tail.Args["key"]);

        var gold = DeterministicAiParser.Parse("gold less shiny");
        Assert.Equal("valid", gold.Status);
        Assert.Equal("material.pbr", gold.Operation);
        Assert.Equal("0.55", gold.Args["metallic"]);
    }

    [Fact]
    public async Task AiEditExecutor_AppliesSoftGoldPbr()
    {
        var target = new AiEditTarget
        {
            Material = new MaterialDefinition { MetallicFactor = 1f, RoughnessFactor = 0.1f }
        };
        var stack = new ThreeDGod.Core.Editing.CommandStack();
        var plan = DeterministicAiParser.Parse("gold less shiny");
        await AiEditExecutor.ExecuteAsync([plan], target, stack);
        Assert.Equal(0.55f, target.Material.MetallicFactor, 2);
        Assert.Equal(0.45f, target.Material.RoughnessFactor, 2);
        Assert.InRange(target.Material.BaseColorFactor.R, 0.8f, 0.9f);
    }

    [Fact]
    public void GodProjectManifest_MinimumAppVersion_MatchesProduct2()
    {
        var manifest = new ThreeDGod.Persistence.GodProjectManifest();
        Assert.Equal("2.0.0", manifest.MinimumAppVersion);
    }

    [Fact]
    public void Composition_RegistersMeshProcessorAndHonestAutoRigGate()
    {
        var services = new ServiceCollection();
        services.AddThreeDGodCoreServices();
        using var sp = services.BuildServiceProvider();
        var processor = sp.GetRequiredService<IMeshProcessor>();
        Assert.Equal(MeshProcessorSelector.DefaultBackendId, processor.BackendId);
        var autoRig = sp.GetRequiredService<IAutoRigBackend>();
        Assert.Equal(FeatureAvailability.NotInstalled, autoRig.Probe());
        Assert.Contains("not installed", autoRig.ProbeMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NonHumanWorkflow_RemeshBindAttachSaveRoundtrip()
    {
        using var dir = new TempDir();
        var (pos, idx) = UnitGrid(8);
        // Slightly elongate into a creature-ish blob
        for (var i = 0; i < pos.Count; i++)
            pos[i] = new Vector3(pos[i].X * 0.4f, pos[i].Y + 0.2f + pos[i].Z * 0.3f, pos[i].Z * 0.8f - 0.2f);

        var remeshed = MeshProcessorSelector.Create().Process(pos, idx, RemeshProfile.CharacterCandidate);
        var skinnedPath = Path.Combine(dir.Path, "creature.glb");
        FreeformCreatureRig.WriteSkinned(skinnedPath, remeshed.Positions, remeshed.Indices);
        Assert.True(File.Exists(skinnedPath));

        var bundle = new ProjectBundle
        {
            Project = new ProjectDocument
            {
                Name = "RatKnight",
                AppVersionCreated = "2.0.0",
                AppVersionLastSaved = "2.0.0"
            }
        };
        var character = new CharacterDocument
        {
            Name = "Sir Squeaks",
            CharacterKind = CharacterKind.FreeformCreature,
            SourceRepresentation = SourceRepresentation.GeneratedMesh
        };
        bundle.Characters.Add(character);
        var horn = AttachmentSockets.Attach(character.CharacterId, Guid.NewGuid(), AttachmentType.Horn);
        var tail = AttachmentSockets.Attach(character.CharacterId, Guid.NewGuid(), AttachmentType.Tail);
        bundle.Attachments.Add(horn);
        bundle.Attachments.Add(tail);

        var archive = new ThreeDGod.Persistence.GodProjectArchive();
        var path = Path.Combine(dir.Path, "rat.3dgod");
        await archive.SaveAsync(bundle, path);
        var loaded = await archive.LoadAsync(path);
        Assert.Equal(2, loaded.Attachments.Count);
        Assert.Contains(loaded.Attachments, a => a.AttachmentType == AttachmentType.Horn);
        Assert.Contains(loaded.Attachments, a => a.AttachmentType == AttachmentType.Tail);
        Assert.Equal(CharacterKind.FreeformCreature, loaded.Characters.Single().CharacterKind);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = Directory.CreateDirectory(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tdg-safe-" + Guid.NewGuid().ToString("N"))).FullName;
        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { /* best effort */ }
        }
    }
}
