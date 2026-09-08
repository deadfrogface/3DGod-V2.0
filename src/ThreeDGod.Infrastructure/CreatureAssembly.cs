using System.Numerics;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Mesh;

namespace ThreeDGod.Infrastructure;

public sealed class CreatureAssembly : ICreatureAssembly
{
    public CharacterDocument AttachHumanTailAndHorns(ProjectBundle bundle, string meshRoot)
    {
        Directory.CreateDirectory(meshRoot);
        var tail = WritePart(bundle, meshRoot, "tail", SemanticBodyPartType.Tail, "tail.base", "boundary:hips.posterior",
            builder => builder.AddTorus(new Vector3(0, 0, -0.18f), Vector3.UnitX, 0.14f, 0.035f, 24, 10));
        var hornL = WritePart(bundle, meshRoot, "horn.L", SemanticBodyPartType.Horn, "head", "boundary:head.temple.L",
            builder => builder.AddCone(new Vector3(-0.08f, 1.72f, 0.02f), new Vector3(-0.16f, 1.95f, -0.02f), 0.03f, 12));
        var hornR = WritePart(bundle, meshRoot, "horn.R", SemanticBodyPartType.Horn, "head", "boundary:head.temple.R",
            builder => builder.AddCone(new Vector3(0.08f, 1.72f, 0.02f), new Vector3(0.16f, 1.95f, -0.02f), 0.03f, 12));

        var character = new CharacterDocument
        {
            Name = "Human + Tail + Horns",
            CharacterKind = CharacterKind.HumanoidCreature,
            SourceRepresentation = SourceRepresentation.ModularCreature,
            CreatureState = new CreatureState
            {
                BaseFamily = "human",
                SkeletonProfileId = "humanoid+tail",
                TopologyCompatibilityGroup = "humanoid-modular",
                RigStrategy = "humanoid-plus-custom",
                BodyPlan = new BodyPlan
                {
                    IsBiped = true,
                    TailCount = 1,
                    ExtraLimbCount = 0,
                    SemanticLimbDescriptors = ["biped", "tail", "horns"],
                    CustomTags = ["skeleton:tail.base", "slots:horn.L", "slots:horn.R"]
                },
                ExtraBodyParts = [tail.Slot, hornL.Slot, hornR.Slot]
            }
        };
        character.MeshSet.MeshAssetIds.AddRange([tail.Asset.MeshAssetId, hornL.Asset.MeshAssetId, hornR.Asset.MeshAssetId]);
        bundle.Characters.Add(character);
        bundle.Project.CharacterIds.Add(character.CharacterId);
        return character;
    }

    private static (BodyPartSlot Slot, MeshAsset Asset) WritePart(
        ProjectBundle bundle,
        string meshRoot,
        string name,
        SemanticBodyPartType type,
        string parentBone,
        string boundary,
        Action<MeshBuilder3D> build)
    {
        var builder = new MeshBuilder3D();
        build(builder);
        var glb = Path.Combine(meshRoot, name.Replace('.', '_') + ".glb");
        TriangleMeshExport.WriteGlb(glb, builder.Positions, builder.Indices);
        var doc = CanonicalGltfPipeline.Load(glb);
        if (doc.VertexCount < 8 || doc.TriangleCount < 8)
            throw new InvalidOperationException($"Creature part '{name}' mesh is too small.");
        var asset = new MeshAsset
        {
            Name = name,
            SourceFormat = "glb",
            CanonicalGlbPath = glb,
            VertexCount = doc.VertexCount,
            TriangleCount = doc.TriangleCount,
            ValidationState = "creature-part"
        };
        bundle.Meshes.Add(asset);
        bundle.Project.AssetIds.Add(asset.MeshAssetId);
        var slot = new BodyPartSlot
        {
            SemanticType = type,
            MeshAssetId = asset.MeshAssetId,
            ParentBoneSemantic = parentBone,
            BoundaryDefinition = boundary,
            RigBinding = parentBone,
            LocalTransform = new SpatialTransform()
        };
        return (slot, asset);
    }
}
