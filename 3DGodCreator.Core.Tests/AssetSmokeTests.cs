namespace ThreeDGodCreator.Core.Tests;

public class AssetSmokeTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Theory]
    [InlineData("view_preview", "skin.png")]
    [InlineData("view_preview", "skin_fat.png")]
    [InlineData("view_preview", "skin_fat_muscle.png")]
    [InlineData("view_preview", "skin_fat_muscle_bone_organs.png")]
    [InlineData("view_overlay/clothes", "clothes_demo_asset.png")]
    [InlineData("view_overlay/piercings", "piercings_demo_asset.png")]
    [InlineData("view_overlay/tattoos", "tattoos_demo_asset.png")]
    public void PreviewAndOverlayPngs_AreValidPngFiles(string relativeDir, string fileName)
    {
        var path = Path.Combine(RepoPaths.AssetsDir, relativeDir.Replace('/', Path.DirectorySeparatorChar), fileName);
        Assert.True(File.Exists(path), path + " missing from repository.");

        var bytes = File.ReadAllBytes(path);
        var looksLikePng = bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(PngSignature);
        Assert.True(looksLikePng, path + " must be a real PNG signature after Safe Reuse asset repair.");
        Assert.True(bytes.Length >= 64, path + " is unexpectedly tiny for a preview PNG.");
    }

    [Fact]
    public void ProductBaseGlbFiles_Exist()
    {
        Assert.True(File.Exists(Path.Combine(RepoPaths.AssetsDir, "characters", "male_base.glb")));
        Assert.True(File.Exists(Path.Combine(RepoPaths.AssetsDir, "characters", "female_base.glb")));
    }
}
