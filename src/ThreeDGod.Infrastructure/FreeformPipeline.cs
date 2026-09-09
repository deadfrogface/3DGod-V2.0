using System.Numerics;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Mesh;
using ThreeDGod.Rigging;

namespace ThreeDGod.Infrastructure;

public sealed class FreeformPipeline : IFreeformCharacterPipeline
{
    private readonly IReferenceImageGenerationService _images;
    private readonly IImageTo3DService _to3d;

    public FreeformPipeline(IReferenceImageGenerationService images, IImageTo3DService to3d)
    {
        _images = images;
        _to3d = to3d;
    }

    public async Task<CharacterDocument> RunAsync(string prompt, ProjectBundle bundle, string workRoot, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(workRoot);
        try
        {
            await _images.GenerateAsync(prompt, seed: 1, bundle, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // FLUX/Qwen remain NotInstalled.
        }

        if (!IsCatalogFreeform(prompt))
        {
            var status = _to3d.ProbeMessage();
            throw new InvalidOperationException(
                "NotInstalled – no ImageTo3D backend and prompt is not a catalog freeform. " + status);
        }

        var high = BuildDragon();
        var sourceGlb = Path.Combine(workRoot, "source.glb");
        TriangleMeshExport.WriteGlb(sourceGlb, high.Positions, high.Indices);
        var sourceTris = high.Indices.Count / 3;

        var remeshed = RemeshPipeline.Run(high.Positions, high.Indices, RemeshProfile.Preview);
        var cleanedGlb = Path.Combine(workRoot, "cleaned.glb");
        TriangleMeshExport.WriteGlb(cleanedGlb, remeshed.Positions, remeshed.Indices, remeshed.Uvs);

        var riggedGlb = Path.Combine(workRoot, "rigged.glb");
        FreeformCreatureRig.WriteSkinned(riggedGlb, remeshed.Positions, remeshed.Indices);
        var report = RigValidator.ValidateGlb(riggedGlb, requireHumanoid: false);
        if (!report.Passed)
            throw new InvalidOperationException("Freeform rig failed validation: " + string.Join("; ", report.Failures.Select(f => f.Code)));

        var mapped = SemanticBoneMap.MapAll(report.JointNames);
        var doc = CanonicalGltfPipeline.Load(riggedGlb);
        var mesh = new MeshAsset
        {
            Name = "freeform-dragon",
            CanonicalGlbPath = riggedGlb,
            VertexCount = doc.VertexCount,
            TriangleCount = doc.TriangleCount,
            HasSkin = doc.SkinCount > 0,
            UvSetCount = doc.HasUv ? 1 : 0,
            ValidationState = "freeform-pipeline",
            GeneratedMetadata = new GeneratedAssetMetadata
            {
                BackendId = "procedural-catalog",
                Prompt = prompt,
                LicenseProfileId = "cc0",
                Parameters =
                {
                    ["sourceTriangles"] = sourceTris.ToString(),
                    ["cleanedTriangles"] = remeshed.Indices.Count.ToString(),
                    ["rig"] = "authored-freeform",
                    ["imageTo3d"] = "NotInstalled"
                }
            }
        };
        bundle.Meshes.Add(mesh);
        var character = new CharacterDocument
        {
            Name = "Kleiner Drache",
            CharacterKind = CharacterKind.FreeformCreature,
            SourceRepresentation = SourceRepresentation.GeneratedMesh,
            CreatureState = new CreatureState
            {
                BaseFamily = "dragon",
                SkeletonProfileId = "freeform",
                RigStrategy = "authored-freeform",
                BodyPlan = new BodyPlan
                {
                    IsBiped = false,
                    TailCount = 1,
                    SemanticLimbDescriptors = ["freeform", "tail", "head"],
                    CustomTags = mapped.Values.Distinct().ToList()
                }
            },
            GeneratedAssetMetadata = mesh.GeneratedMetadata
        };
        character.MeshSet.MeshAssetIds.Add(mesh.MeshAssetId);
        bundle.Characters.Add(character);
        bundle.Project.CharacterIds.Add(character.CharacterId);
        return character;
    }

    public static bool IsCatalogFreeform(string prompt)
    {
        var p = prompt.Trim().ToLowerInvariant();
        return p.Contains("drache") || p.Contains("dragon") || p.Contains("freeform");
    }

    public static (List<Vector3> Positions, List<int> Indices) BuildDragon()
    {
        var b = new MeshBuilder3D();
        b.AddSphere(new Vector3(0, 0.22f, 0), 0.22f, 28, 20);
        b.AddSphere(new Vector3(0, 0.38f, 0.2f), 0.12f, 20, 14);
        b.AddCone(new Vector3(0, 0.36f, 0.28f), new Vector3(0, 0.32f, 0.48f), 0.05f, 12);
        b.AddTorus(new Vector3(0, 0.18f, -0.22f), Vector3.UnitX, 0.16f, 0.04f, 20, 10);
        b.AddSphere(new Vector3(-0.12f, 0.02f, 0.1f), 0.05f, 10, 8);
        b.AddSphere(new Vector3(0.12f, 0.02f, 0.1f), 0.05f, 10, 8);
        b.AddSphere(new Vector3(-0.12f, 0.02f, -0.08f), 0.05f, 10, 8);
        b.AddSphere(new Vector3(0.12f, 0.02f, -0.08f), 0.05f, 10, 8);
        return (b.Positions, b.Indices);
    }
}
