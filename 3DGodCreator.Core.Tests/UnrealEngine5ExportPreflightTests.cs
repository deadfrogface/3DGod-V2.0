using System.Numerics;
using ThreeDGod.Export;
using ThreeDGod.Mesh;

namespace ThreeDGodCreator.Core.Tests;

public class UnrealEngine5ExportPreflightTests
{
    [Fact]
    public void CompleteScene_PassesHardChecks_WithSoftNotes()
    {
        var path = Path.Combine(Path.GetTempPath(), "ue5-ok-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            GlbExportService.WriteCompleteSceneStatic(path);
            var report = UnrealEngine5ExportProfile.EvaluateGlb(path, assetName: "SK_Character");
            Assert.True(report.Passed, string.Join("; ", report.HardMessages));
            Assert.Contains(report.Issues, i => i.Category == "cm scale");
            Assert.Contains(report.Issues, i => i.Code == "ImportNotClaimed");
            Assert.Contains(report.Issues, i => i.Category == "skeleton" || i.Category == "weights" || i.Code == "MorphPresent");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void MissingFile_IsHardFail()
    {
        var report = UnrealEngine5ExportProfile.EvaluateGlb(Path.Combine(Path.GetTempPath(), "nope-" + Guid.NewGuid().ToString("N") + ".glb"));
        Assert.False(report.Passed);
        Assert.Contains(report.Issues, i => i.Code == "MissingAsset");
    }

    [Fact]
    public void TinyCorruptFile_IsHardFail()
    {
        var path = Path.Combine(Path.GetTempPath(), "ue5-tiny-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            File.WriteAllBytes(path, [1, 2, 3]);
            var report = UnrealEngine5ExportProfile.EvaluateGlb(path, assetName: "SK_Bad");
            Assert.False(report.Passed);
            Assert.Contains(report.Issues, i => i.Code is "TooSmall" or "Unreadable");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void MeshWithoutSkin_IsHardFail_WhenSkinRequired()
    {
        var path = Path.Combine(Path.GetTempPath(), "ue5-noskin-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            var b = new MeshBuilder3D();
            b.AddSphere(Vector3.Zero, 0.5f, 8, 6);
            TriangleMeshExport.WriteGlb(path, b.Positions, b.Indices);
            var report = UnrealEngine5ExportProfile.EvaluateGlb(path, assetName: "SM_Prop", requireSkin: true);
            Assert.False(report.Passed);
            Assert.Contains(report.Issues, i => i.Code == "NoSkin" || i.Category == "skeleton");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void InvalidAssetName_IsHardFail()
    {
        var path = Path.Combine(Path.GetTempPath(), "ue5-name-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            GlbExportService.WriteCompleteSceneStatic(path);
            var report = UnrealEngine5ExportProfile.EvaluateGlb(path, assetName: "bad name!");
            Assert.False(report.Passed);
            Assert.Contains(report.Issues, i => i.Code == "InvalidName");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void MaleBase_Preflight_DoesNotCrash()
    {
        var src = Path.Combine(RepoPaths.AssetsDir, "characters", "male_base.glb");
        Assert.True(File.Exists(src));
        var report = UnrealEngine5ExportProfile.EvaluateGlb(src, assetName: "SK_MaleBase", requireSkin: true);
        // May pass or fail depending on skin quality – must always produce categorized issues.
        Assert.NotEmpty(report.Issues);
        Assert.Contains(report.Issues, i => i.Code == "ImportNotClaimed");
    }
}
