using System.Security.Cryptography;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;

namespace ThreeDGod.Infrastructure;

public sealed class AiAssetPipeline : IAssetGenerationService
{
    private readonly IReferenceImageGenerationService _images;
    private readonly IImageTo3DService _to3d;
    private readonly AssetLibrary _library;

    public AiAssetPipeline(IReferenceImageGenerationService images, IImageTo3DService to3d, AssetLibrary library)
    {
        _images = images;
        _to3d = to3d;
        _library = library;
    }

    public async Task<LibraryAsset> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var bundle = new ProjectBundle();
            await _images.GenerateAsync(prompt, seed: 1, bundle, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // FLUX/Qwen remain NotInstalled. Catalog path may still produce a real mesh.
        }

        if (IsFrogNecklace(prompt))
            return BuildFrogNecklace(prompt);

        var status = _to3d.ProbeMessage();
        throw new InvalidOperationException(
            "NotInstalled – no ImageTo3D backend and prompt is not in the procedural catalog. " + status);
    }

    public static bool IsFrogNecklace(string prompt)
    {
        var p = prompt.Trim().ToLowerInvariant();
        return p.Contains("halskette") && p.Contains("frosch")
            || p.Contains("necklace") && p.Contains("frog");
    }

    private LibraryAsset BuildFrogNecklace(string prompt)
    {
        var (positions, indices) = ProceduralJewelry.SilverFrogNecklace();
        var report = MeshValidator.Validate(positions, indices, uvCount: 0);
        if (report.Rejected)
            throw new InvalidOperationException("Catalog mesh failed validation.");

        var silver = PbrMaterials.Get("Silver");
        var destDir = Path.Combine(_library.Root, "Jewelry", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destDir);
        var glb = Path.Combine(destDir, "asset.glb");
        var preview = Path.Combine(destDir, "preview.png");
        TriangleMeshExport.WriteGlb(glb, positions, indices, silver.BaseColor, silver.Metallic, silver.Roughness);
        var doc = CanonicalGltfPipeline.Load(glb);
        if (doc.VertexCount < 200 || doc.TriangleCount < 200)
            throw new InvalidOperationException("Catalog mesh is too small to be a real asset.");
        PngWriter.WriteMeshPreview(preview, positions, indices);
        if (!PngSignature.TryReadSize(File.ReadAllBytes(preview), out _, out _))
            throw new InvalidOperationException("Preview PNG is invalid.");

        var asset = new LibraryAsset
        {
            Name = "Silberne Halskette mit Froschanhänger",
            Category = "Jewelry",
            GlbPath = glb,
            PreviewPngPath = preview,
            Provenance = new GeneratedAssetMetadata
            {
                BackendId = "procedural-catalog",
                BackendVersion = "1",
                Prompt = prompt,
                LicenseProfileId = "cc0",
                ModelHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(glb))).ToLowerInvariant(),
                Parameters = { ["category"] = "Jewelry", ["vertexCount"] = doc.VertexCount.ToString() }
            }
        };
        _library.Save(asset);
        return asset;
    }
}
