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

    public CharacterDocument CreateOrc(ProjectBundle bundle, string meshRoot)
    {
        Directory.CreateDirectory(meshRoot);
        var earL = WritePart(bundle, meshRoot, "ear.L", SemanticBodyPartType.Ear, "head", "boundary:head.ear.L",
            builder => builder.AddCone(new Vector3(-0.12f, 1.68f, 0), new Vector3(-0.28f, 1.88f, -0.02f), 0.045f, 10));
        var earR = WritePart(bundle, meshRoot, "ear.R", SemanticBodyPartType.Ear, "head", "boundary:head.ear.R",
            builder => builder.AddCone(new Vector3(0.12f, 1.68f, 0), new Vector3(0.28f, 1.88f, -0.02f), 0.045f, 10));
        var tuskL = WritePart(bundle, meshRoot, "tusk.L", SemanticBodyPartType.Tusk, "head", "boundary:jaw.canine.L",
            builder => builder.AddCone(new Vector3(-0.04f, 1.58f, 0.08f), new Vector3(-0.07f, 1.48f, 0.16f), 0.012f, 8));
        var tuskR = WritePart(bundle, meshRoot, "tusk.R", SemanticBodyPartType.Tusk, "head", "boundary:jaw.canine.R",
            builder => builder.AddCone(new Vector3(0.04f, 1.58f, 0.08f), new Vector3(0.07f, 1.48f, 0.16f), 0.012f, 8));

        var bodyGlb = Path.Combine(meshRoot, "orc-body.glb");
        ThreeDGod.Rigging.HumanoidTestRig.WriteGood(bodyGlb);
        var bodyDoc = CanonicalGltfPipeline.Load(bodyGlb);
        var body = new MeshAsset
        {
            Name = "orc-body",
            CanonicalGlbPath = bodyGlb,
            VertexCount = bodyDoc.VertexCount,
            TriangleCount = bodyDoc.TriangleCount,
            HasSkin = bodyDoc.SkinCount > 0,
            ValidationState = "orc-body"
        };
        bundle.Meshes.Add(body);

        var orcSkin = PbrMaterials.Get("OrcSkin");
        var material = PbrMaterials.ToDefinition(orcSkin);
        bundle.Materials.Add(material);

        var character = new CharacterDocument
        {
            Name = "Orc",
            CharacterKind = CharacterKind.HumanoidCreature,
            SourceRepresentation = SourceRepresentation.ModularCreature,
            ParametricHumanState = new ParametricHumanState
            {
                BackendId = "anny",
                TopologyProfile = "anny",
                PhenotypeParameters =
                {
                    ["muscle"] = 0.92f,
                    ["weight"] = 0.7f,
                    ["proportions"] = 0.35f,
                    ["height"] = 0.62f
                }
            },
            CreatureState = new CreatureState
            {
                BaseFamily = "orc",
                SkeletonProfileId = "humanoid",
                TopologyCompatibilityGroup = "humanoid-modular",
                RigStrategy = "humanoid",
                BodyPlan = new BodyPlan
                {
                    IsBiped = true,
                    SemanticLimbDescriptors = ["biped", "orc-ears", "tusks"],
                    CustomTags = ["family:orc"]
                },
                ExtraBodyParts = [earL.Slot, earR.Slot, tuskL.Slot, tuskR.Slot]
            }
        };
        character.MeshSet.MeshAssetIds.AddRange(
            [body.MeshAssetId, earL.Asset.MeshAssetId, earR.Asset.MeshAssetId, tuskL.Asset.MeshAssetId, tuskR.Asset.MeshAssetId]);
        character.MaterialSet.MaterialIds.Add(material.MaterialId);
        bundle.Characters.Add(character);
        bundle.Project.CharacterIds.Add(character.CharacterId);
        return character;
    }

    public CharacterDocument CreateRat(ProjectBundle bundle, string meshRoot)
    {
        Directory.CreateDirectory(meshRoot);
        var muzzle = WritePart(bundle, meshRoot, "muzzle", SemanticBodyPartType.Head, "head", "boundary:head.face.muzzle",
            builder => builder.AddCone(new Vector3(0, 1.62f, 0.08f), new Vector3(0, 1.58f, 0.28f), 0.045f, 14));
        var earL = WritePart(bundle, meshRoot, "ear.L", SemanticBodyPartType.Ear, "head", "boundary:head.ear.L",
            builder => builder.AddSphere(new Vector3(-0.14f, 1.78f, 0), 0.055f, 10, 8));
        var earR = WritePart(bundle, meshRoot, "ear.R", SemanticBodyPartType.Ear, "head", "boundary:head.ear.R",
            builder => builder.AddSphere(new Vector3(0.14f, 1.78f, 0), 0.055f, 10, 8));
        var tail = WritePart(bundle, meshRoot, "tail", SemanticBodyPartType.Tail, "tail.base", "boundary:hips.posterior",
            builder => builder.AddTorus(new Vector3(0, 0.02f, -0.28f), Vector3.UnitX, 0.22f, 0.025f, 28, 10));

        var bodyGlb = Path.Combine(meshRoot, "rat-body.glb");
        ThreeDGod.Rigging.HumanoidTestRig.WriteGood(bodyGlb);
        var bodyDoc = CanonicalGltfPipeline.Load(bodyGlb);
        var body = new MeshAsset
        {
            Name = "rat-body",
            CanonicalGlbPath = bodyGlb,
            VertexCount = bodyDoc.VertexCount,
            TriangleCount = bodyDoc.TriangleCount,
            HasSkin = bodyDoc.SkinCount > 0,
            ValidationState = "rat-body"
        };
        bundle.Meshes.Add(body);

        var character = new CharacterDocument
        {
            Name = "Humanoid Rat",
            CharacterKind = CharacterKind.HumanoidCreature,
            SourceRepresentation = SourceRepresentation.ModularCreature,
            ParametricHumanState = new ParametricHumanState
            {
                BackendId = "anny",
                TopologyProfile = "anny",
                PhenotypeParameters = { ["height"] = 0.28f, ["proportions"] = 0.2f, ["weight"] = 0.35f }
            },
            CreatureState = new CreatureState
            {
                BaseFamily = "rat",
                SkeletonProfileId = "humanoid+tail",
                TopologyCompatibilityGroup = "humanoid-modular",
                RigStrategy = "humanoid-plus-custom",
                BodyPlan = new BodyPlan
                {
                    IsBiped = true,
                    TailCount = 1,
                    SemanticLimbDescriptors = ["biped", "muzzle", "rat-ears", "tail"],
                    CustomTags = ["family:rat", "skeleton:tail.base"]
                },
                ExtraBodyParts = [muzzle.Slot, earL.Slot, earR.Slot, tail.Slot]
            }
        };
        character.MeshSet.MeshAssetIds.AddRange(
            [body.MeshAssetId, muzzle.Asset.MeshAssetId, earL.Asset.MeshAssetId, earR.Asset.MeshAssetId, tail.Asset.MeshAssetId]);
        bundle.Characters.Add(character);
        bundle.Project.CharacterIds.Add(character.CharacterId);
        return character;
    }

    public CharacterDocument CreateEditableHumanoid(ProjectBundle bundle, string meshRoot)
    {
        Directory.CreateDirectory(meshRoot);
        var head = WritePart(bundle, meshRoot, "human-head", SemanticBodyPartType.Head, "head", "boundary:head.base",
            builder => builder.AddSphere(new Vector3(0, 1.7f, 0), 0.11f, 14, 10));
        var handL = WritePart(bundle, meshRoot, "hand.L", SemanticBodyPartType.LeftHand, "hand.L", "boundary:wrist.L",
            builder => builder.AddSphere(new Vector3(-0.7f, 1.4f, 0), 0.05f, 10, 8));
        var handR = WritePart(bundle, meshRoot, "hand.R", SemanticBodyPartType.RightHand, "hand.R", "boundary:wrist.R",
            builder => builder.AddSphere(new Vector3(0.7f, 1.4f, 0), 0.05f, 10, 8));
        var bodyGlb = Path.Combine(meshRoot, "humanoid-body.glb");
        ThreeDGod.Rigging.HumanoidTestRig.WriteGood(bodyGlb);
        var bodyDoc = CanonicalGltfPipeline.Load(bodyGlb);
        var body = new MeshAsset
        {
            Name = "humanoid-body",
            CanonicalGlbPath = bodyGlb,
            VertexCount = bodyDoc.VertexCount,
            TriangleCount = bodyDoc.TriangleCount,
            HasSkin = bodyDoc.SkinCount > 0,
            ValidationState = "editable-humanoid"
        };
        bundle.Meshes.Add(body);
        var character = new CharacterDocument
        {
            Name = "Editable Humanoid",
            CharacterKind = CharacterKind.HumanoidCreature,
            SourceRepresentation = SourceRepresentation.ModularCreature,
            CreatureState = new CreatureState
            {
                BaseFamily = "human",
                SkeletonProfileId = "humanoid",
                BodyPlan = new BodyPlan { IsBiped = true, SemanticLimbDescriptors = ["biped"] },
                ExtraBodyParts = [head.Slot, handL.Slot, handR.Slot]
            }
        };
        character.MeshSet.MeshAssetIds.AddRange(
            [body.MeshAssetId, head.Asset.MeshAssetId, handL.Asset.MeshAssetId, handR.Asset.MeshAssetId]);
        bundle.Characters.Add(character);
        bundle.Project.CharacterIds.Add(character.CharacterId);
        return character;
    }

    internal static (BodyPartSlot Slot, MeshAsset Asset) WritePart(
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
