using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;

namespace ThreeDGodCreator.Core.Tests;

public class AssetPipelineTests
{
    [Fact]
    public async Task FrogNecklacePrompt_CreatesRealAssetWithProvenanceAndPreview()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-lib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var pipeline = new AiAssetPipeline(new ReferenceImageService(), new ImageTo3DService(), new AssetLibrary(root));
            var asset = await pipeline.GenerateAsync("Silberne Halskette mit Froschanhänger");
            Assert.Equal("Jewelry", asset.Category);
            Assert.Equal("procedural-catalog", asset.Provenance.BackendId);
            Assert.Contains("Halskette", asset.Provenance.Prompt, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(asset.GlbPath));
            Assert.True(File.Exists(asset.PreviewPngPath));
            var doc = CanonicalGltfPipeline.Load(asset.GlbPath);
            Assert.True(doc.VertexCount > 200);
            Assert.True(doc.TriangleCount > 200);
            Assert.True(PngSignature.TryReadSize(File.ReadAllBytes(asset.PreviewPngPath!), out var w, out var h));
            Assert.True(w >= 8 && h >= 8);
            var listed = new AssetLibrary(root).List("Jewelry");
            Assert.Contains(listed, a => a.LibraryAssetId == asset.LibraryAssetId);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task UnknownPrompt_DoesNotWriteMesh()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-lib-" + Guid.NewGuid().ToString("N"));
        var destGuess = Path.Combine(root, "nope.glb");
        try
        {
            var pipeline = new AiAssetPipeline(new ReferenceImageService(), new ImageTo3DService(), new AssetLibrary(root));
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.GenerateAsync("drop table characters"));
            Assert.Contains("NotInstalled", ex.Message, StringComparison.Ordinal);
            Assert.False(File.Exists(destGuess));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Necklace_FollowsNeckOnTestPose()
    {
        var characterId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var attachment = AttachmentKinematics.Bind(
            characterId,
            assetId,
            AttachmentType.Necklace,
            new SpatialTransform { Ty = 0.05 });
        Assert.Equal(SemanticSlots.Neck, attachment.ParentBoneSemantic);

        var rest = new SpatialTransform { Tx = 0, Ty = 1.4, Tz = 0.08 };
        var posed = AttachmentKinematics.RotateY(rest, 40);
        var worldRest = AttachmentKinematics.WorldOnPose(attachment, rest);
        var worldPosed = AttachmentKinematics.WorldOnPose(attachment, posed);
        Assert.NotEqual(worldRest.Tx, worldPosed.Tx);
        Assert.Equal(worldRest.Ty, worldPosed.Ty, 5);
        Assert.Equal(SemanticSlots.HandRight, AttachmentDefaults.SlotFor(AttachmentType.Weapon));
        Assert.Equal(SemanticSlots.EarLeft, AttachmentDefaults.SlotFor(AttachmentType.Earring));
    }

    [Fact]
    public void GoldLessShiny_ChangesRoughness_AndSurvivesGlbReload()
    {
        var (positions, indices) = ProceduralJewelry.SilverFrogNecklace();
        var glb = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            var silver = PbrMaterials.Get("Silver");
            TriangleMeshExport.WriteGlb(glb, positions, indices, silver.BaseColor, silver.Metallic, silver.Roughness);
            var edited = PbrMaterials.FromPrompt("Kette Gold, weniger glänzend");
            Assert.Equal("Gold", edited.Name);
            Assert.True(edited.Roughness > PbrMaterials.Get("Gold").Roughness);
            PbrMaterials.ApplyToGlb(glb, edited);
            var read = PbrMaterials.ReadFirst(glb);
            Assert.True(read.Metallic > 0.5f);
            Assert.InRange(read.Roughness, 0.35f, 0.6f);
            Assert.InRange(read.Color.X, 0.7f, 1f);
            var material = PbrMaterials.ToDefinition(edited);
            var json = DomainJson.Roundtrip(material);
            Assert.Equal(edited.Roughness, json.RoughnessFactor, 3);
            var fromHex = PbrMaterials.FromHex("Gold", "#D4AF37", 1f, 0.45f);
            Assert.InRange(fromHex.BaseColor.X, 0.7f, 1f);
        }
        finally
        {
            if (File.Exists(glb)) File.Delete(glb);
        }
    }
}
