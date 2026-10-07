using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Mesh;

namespace ThreeDGod.Infrastructure;

/// <summary>
/// Replaces development-only procedural creature parts with installed, verified CC0 mesh assets.
/// It deliberately does not apply hm08 .target morphs to Anny topology.
/// </summary>
public sealed class Cc0CreaturePartReplacementService
{
    private readonly Cc0CreatureAssetPackService _assets;

    public Cc0CreaturePartReplacementService(Cc0CreatureAssetPackService assets) => _assets = assets;

    public bool CanReplaceOrc =>
        _assets.IsInstalled(Cc0CreatureAssetPackService.Bodyparts01)
        && _assets.FindObj("culturalibre_minotaur_horns") is not null;

    public void ReplaceOrcDevelopmentParts(ActiveProjectSession session, string workRoot)
    {
        var snapshot = session.Snapshot();
        var character = snapshot.Characters.FirstOrDefault(c => c.CharacterId == session.ActiveCharacter?.CharacterId)
            ?? throw new InvalidOperationException("No active character.");
        if (!string.Equals(character.CreatureState?.BaseFamily, "orc", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CC0 Orc replacement requires an active Orc.");
        if (!CanReplaceOrc)
            throw new InvalidOperationException("Verified CC0 Bodyparts 01 is not installed or Minotaur Horns OBJ is missing.");

        Directory.CreateDirectory(workRoot);
        var glb = Path.Combine(workRoot, "cc0-culturalibre-minotaur-horns.glb");
        _assets.ConvertObjAssetToGlb("culturalibre_minotaur_horns", glb);
        var doc = CanonicalGltfPipeline.Load(glb);
        if (doc.VertexCount < 8 || doc.TriangleCount < 8)
            throw new InvalidDataException("Converted CC0 horn asset did not contain a usable mesh.");

        // Remove only development-generated modular parts from this creature.
        var oldIds = character.CreatureState!.ExtraBodyParts
            .Where(x => x.MeshAssetId.HasValue)
            .Select(x => x.MeshAssetId!.Value)
            .ToHashSet();
        character.CreatureState.ExtraBodyParts.Clear();
        character.MeshSet.MeshAssetIds.RemoveAll(oldIds.Contains);
        snapshot.Meshes.RemoveAll(m => oldIds.Contains(m.MeshAssetId));
        foreach (var id in oldIds) snapshot.MeshBytes.Remove(id);

        var mesh = new MeshAsset
        {
            Name = "CC0 Minotaur Horns (culturalibre)",
            SourceFormat = "glb",
            CanonicalGlbPath = glb,
            VertexCount = doc.VertexCount,
            TriangleCount = doc.TriangleCount,
            ValidationState = "cc0-verified:makehuman-bodyparts01"
        };
        snapshot.Meshes.Add(mesh);
        snapshot.MeshBytes[mesh.MeshAssetId] = File.ReadAllBytes(glb);
        snapshot.Project.AssetIds.Add(mesh.MeshAssetId);
        character.MeshSet.MeshAssetIds.Add(mesh.MeshAssetId);
        character.CreatureState.ExtraBodyParts.Add(new BodyPartSlot
        {
            SemanticType = SemanticBodyPartType.Horn,
            MeshAssetId = mesh.MeshAssetId,
            ParentBoneSemantic = "head",
            BoundaryDefinition = "cc0-mhclo:hm08;attachment-retained-as-rigid-mesh",
            RigBinding = "head",
            GenerationProvenance = new GeneratedAssetMetadata
            {
                Provider = "makehuman-community",
                ModelOrVersion = "bodyparts01",
                PromptOrSource = "culturalibre_minotaur_horns",
                License = "CC0-1.0"
            }
        });

        session.LoadCreatedCreature(snapshot, character.CharacterId, session.ProjectPath);
    }
}
