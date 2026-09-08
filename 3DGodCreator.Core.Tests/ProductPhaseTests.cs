using ThreeDGod.AI;
using ThreeDGod.Application;
using ThreeDGod.Export;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Rendering;

namespace ThreeDGodCreator.Core.Tests;

public class ProductPhaseTests
{
    [Fact]
    public void ViewportClick_MapsRenderIdToDomainObject()
    {
        var svc = new ViewportSelectionService();
        var id = Guid.NewGuid();
        svc.Register(7, new ViewportObjectHit { DomainObjectId = id, Kind = "mesh" });
        Assert.Equal(id, svc.SelectRenderId(7));
        svc.EnableSkeletonOverlay(hasRealBones: false);
        Assert.False(svc.SkeletonOverlayEnabled);
        svc.EnableSkeletonOverlay(hasRealBones: true);
        Assert.True(svc.SkeletonOverlayEnabled);
    }

    [Fact]
    public void ViewportDiagnostic_CanHighlightAndClear()
    {
        var hl = new ViewportDiagnosticHighlight { MeshAssetId = Guid.NewGuid(), VertexIndices = [1, 2] };
        hl.Show();
        Assert.True(hl.Active);
        hl.Clear();
        Assert.False(hl.Active);
    }

    [Fact]
    public void GatedWorkers_AreNotInstalled_AndNeverSuccess()
    {
        foreach (var id in GatedWorkerCatalog.AllHeavyWorkers)
        {
            var status = GatedWorkerCatalog.Probe(id);
            Assert.NotEqual(FeatureAvailability.Available, status.Availability);
            Assert.DoesNotContain("success", status.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void LayoutService_ResetsCorruptJson()
    {
        var svc = new LayoutService();
        Assert.Contains("simple", svc.ResetCorrupt("{not json"), StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_DeAndEn()
    {
        Assert.Equal("Rückgängig", LocalizationCatalog.Get("de", "action.undo"));
        Assert.Equal("Undo", LocalizationCatalog.Get("en", "action.undo"));
    }

    [Fact]
    public void LicenseGate_RequiresExplicitAccept()
    {
        var gate = new LicenseGate();
        Assert.False(gate.TryAccept("apache", false));
        Assert.True(gate.TryAccept("apache", true));
    }

    [Fact]
    public void FbxSanity_DoesNotClaimUe5Import()
    {
        var issues = ExportPreflight.FbxSanity(null);
        Assert.Contains(issues, i => i.Contains("UE5", StringComparison.Ordinal));
    }

    [Fact]
    public void GlbExport_PreservesVertexCount()
    {
        var src = Path.Combine(RepoPaths.AssetsDir, "characters", "male_base.glb");
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            GlbExportService.Export(src, dst);
            Assert.True(File.Exists(dst));
            Assert.Equal(CanonicalGltfPipeline.Load(src).VertexCount, CanonicalGltfPipeline.Load(dst).VertexCount);
        }
        finally { if (File.Exists(dst)) File.Delete(dst); }
    }

    [Theory]
    [InlineData("shoulders wider")]
    [InlineData("jacket red")]
    [InlineData("remove necklace")]
    [InlineData("taller, keep head size")]
    [InlineData("gold chain")]
    [InlineData("rat head")]
    public void DeterministicAiParser_NeverEmitsArbitraryCode(string prompt)
    {
        var plan = DeterministicAiParser.Parse(prompt);
        Assert.Equal("valid", plan.Status);
        Assert.False(string.IsNullOrWhiteSpace(plan.Operation));
        Assert.DoesNotContain("eval", DeterministicAiParser.ToJson(plan), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownPrompt_IsUnsupported()
    {
        Assert.Equal("Unsupported", DeterministicAiParser.Parse("drop table characters").Status);
    }

    [Fact]
    public void LodService_ReducesTriangles()
    {
        Assert.True(LodService.TriangleCountForLod(1000, 2) < 1000);
    }

    [Fact]
    public void UniformScale_IsNotAcceptedAsMorph()
    {
        Assert.Equal(FeatureAvailability.NotImplemented, new FeatureAvailabilityService().GetStatus(FeatureIds.HeightMorph));
    }
}
