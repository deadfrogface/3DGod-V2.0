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
        Assert.Equal(id, svc.SelectedDomainObjectId);
        svc.EnableSkeletonOverlay(hasRealBones: false);
        Assert.False(svc.SkeletonOverlayEnabled);
        svc.EnableSkeletonOverlay(hasRealBones: true);
        Assert.True(svc.SkeletonOverlayEnabled);
        svc.ClearSelection();
        Assert.Null(svc.SelectedDomainObjectId);
    }

    [Fact]
    public void ViewportDiagnostic_ResolvesKnownVertexIds_AndClears()
    {
        var meshId = Guid.NewGuid();
        var positions = new[]
        {
            new System.Numerics.Vector3(0, 0, 0),
            new System.Numerics.Vector3(1, 0, 0),
            new System.Numerics.Vector3(0, 1, 0),
            new System.Numerics.Vector3(0, 0, 1)
        };
        var indices = new[] { 0, 1, 2, 0, 2, 3 };
        var hl = new ViewportDiagnosticHighlight();
        hl.Show(new DiagnosticSceneTarget
        {
            MeshAssetId = meshId,
            VertexIndices = [1, 2],
            TriangleIndices = [0]
        }, positions, indices);

        Assert.True(hl.Active);
        Assert.Equal(3, hl.HighlightedPositions.Count); // 2 verts + 1 triangle centroid
        Assert.Equal(positions[1], hl.HighlightedPositions[0]);
        Assert.Equal(positions[2], hl.HighlightedPositions[1]);
        Assert.NotNull(hl.FocusPoint);

        hl.Clear();
        Assert.False(hl.Active);
        Assert.Empty(hl.HighlightedPositions);
        Assert.Null(hl.FocusPoint);
    }

    [Fact]
    public void DiagnosticCapture_AttachesSceneRefs_ForViewportFocus()
    {
        var diagnostics = new ThreeDGod.Core.Diagnostics.DiagnosticService();
        var meshId = Guid.NewGuid();
        var issue = diagnostics.Capture(
            new InvalidOperationException("non-manifold near vertex 2"),
            "Mesh.Validate",
            scene: new ThreeDGod.Core.Diagnostics.DiagnosticSceneRef
            {
                MeshAssetId = meshId,
                VertexIndices = [2],
                WorldPosition = [0.1, 0.2, 0.3]
            });
        Assert.NotNull(issue.Scene);
        Assert.Equal(meshId, issue.Scene!.MeshAssetId);
        Assert.Equal([2], issue.Scene.VertexIndices);
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
    public void LodService_BudgetIsEstimate_BuildLodMeshReducesGeometry()
    {
        Assert.True(LodService.EstimateTriangleBudget(1000, 2) < 1000);
        var b = new MeshBuilder3D();
        b.AddSphere(System.Numerics.Vector3.Zero, 0.5f, 24, 16);
        var sourceTris = b.Indices.Count / 3;
        var lod = LodService.BuildLodMesh(b.Positions, b.Indices, level: 2);
        Assert.True(lod.Indices.Count / 3 < sourceTris);
        Assert.True(lod.Indices.Count % 3 == 0);
        Assert.All(lod.Indices, i => Assert.InRange(i, 0, lod.Positions.Count - 1));
        Assert.Equal("lod-vertex-cluster", lod.BackendId);

        var byRatio = LodService.BuildLodMesh(b.Positions, b.Indices, new LodBuildOptions { TargetRatio = 0.2f });
        Assert.True(byRatio.Indices.Count / 3 < sourceTris);
        Assert.Equal("lod-ratio", byRatio.BackendId);

        var byError = LodService.BuildLodMesh(b.Positions, b.Indices, new LodBuildOptions { ErrorHint = 0.8f });
        Assert.True(byError.Indices.Count / 3 < sourceTris);
        Assert.Equal("lod-error", byError.BackendId);
    }

    [Fact]
    public void UniformScale_IsNotAcceptedAsMorph()
    {
        Assert.Equal(FeatureAvailability.NotImplemented, new FeatureAvailabilityService().GetStatus(FeatureIds.HeightMorph));
    }
}
