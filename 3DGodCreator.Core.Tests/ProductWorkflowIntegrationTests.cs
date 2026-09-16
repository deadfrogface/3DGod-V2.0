using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using SharpGLTF.Schema2;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Export;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Product workflow tests over PRODUCTION services.
/// Persistence-only tests are labeled as such — they do NOT prove GarmentCode/Fit.
/// </summary>
public class ProductWorkflowIntegrationTests
{
    [Fact]
    public async Task GarmentPersistence_Roundtrip_PreservesEmbeddedGarmentMesh()
    {
        // Persistence fidelity only — does NOT invoke GarmentCode / GarmentFit.
        var recovery = Path.Combine(Path.GetTempPath(), "3dgod-wf-rec-" + Guid.NewGuid().ToString("N"));
        var work = Path.Combine(Path.GetTempPath(), "3dgod-wf-work-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(recovery);
        Directory.CreateDirectory(work);
        var projectPath = Path.Combine(work, "hero.3dgod");
        var meshSrc = Path.Combine(work, "body.glb");

        try
        {
            GlbExportService.WriteCompleteSceneStatic(meshSrc);
            var originalBytes = await File.ReadAllBytesAsync(meshSrc);
            Assert.True(originalBytes.Length > 32);

            var session = new ActiveProjectSession();
            var archive = new GodProjectArchive();
            var autosave = new AutosaveService(recovery, debounce: TimeSpan.FromMilliseconds(10), archive);
            var edits = new AllowlistedAiEditExecutor(session);
            var workflow = new ProductWorkflowService(session, archive, edits, autosave, work);

            workflow.NewHumanProject("Hero");
            workflow.ApplyAnnyState(new ParametricHumanState
            {
                BackendId = "anny",
                TopologyProfile = "anny",
                RigProfile = "anny",
                PhenotypeParameters = { ["height"] = 0.62f, ["weight"] = 0.4f }
            });
            workflow.SetBodyMeshBytes(originalBytes);
            workflow.UpsertMaterial("skin", 0.7f, 0.5f, 0.4f, metallic: 0.1f, roughness: 0.55f);

            var edit = await workflow.ApplyAiEditAsync("taller, keep head size");
            Assert.True(edit.Ok, edit.Message);

            // Distinct garment bytes (not empty) — persistence only, NOT clothing E2E.
            var garmentGlb = Path.Combine(work, "garment-persist.glb");
            WriteDistinctBoxGlb(garmentGlb, new Vector3(-0.4f, 0.2f, -0.25f), new Vector3(0.4f, 0.55f, 0.25f));
            var garmentBytes = await File.ReadAllBytesAsync(garmentGlb);
            workflow.AddFittedGarment(garmentGlb, "jacket", new ClippingReport { InsideAfter = 0, MinDistanceAfter = 0.01f });

            await workflow.FlushAutosaveAsync();
            Assert.NotEmpty(autosave.ListRecoveries());

            await workflow.SaveProjectAsync(projectPath);

            var session2 = new ActiveProjectSession();
            var workflow2 = new ProductWorkflowService(
                session2, archive, new AllowlistedAiEditExecutor(session2), workRoot: work);
            await workflow2.OpenProjectAsync(projectPath);

            var loaded = session2.Snapshot();
            Assert.Equal("Hero", loaded.Project.Name);
            Assert.NotEmpty(loaded.MeshBytes);
            Assert.Contains(loaded.MeshBytes.Values, b => b.Length == originalBytes.Length);
            Assert.Contains(loaded.MeshBytes.Values, b => b.Length == garmentBytes.Length);
            Assert.NotEmpty(loaded.GarmentInstances);
            Assert.Equal("fitted", loaded.GarmentInstances[0].FitState);
            Assert.Equal(2, session2.ListSceneParts().Count);
        }
        finally
        {
            try { Directory.Delete(recovery, true); } catch { /* ignore */ }
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task ComposedExport_BodyPlusGarment_ContainsBothMeshes()
    {
        var work = Path.Combine(Path.GetTempPath(), "3dgod-compose-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var bodyGlb = Path.Combine(work, "body.glb");
        var jacketGlb = Path.Combine(work, "jacket.glb");
        var exportPath = Path.Combine(work, "composed.glb");
        try
        {
            GlbExportService.WriteCompleteSceneStatic(bodyGlb);
            WriteDistinctBoxGlb(jacketGlb, new Vector3(-0.35f, 0.15f, -0.2f), new Vector3(0.35f, 0.5f, 0.2f));
            var bodyDoc = CanonicalGltfPipeline.Load(bodyGlb);
            var jacketDoc = CanonicalGltfPipeline.Load(jacketGlb);
            Assert.True(bodyDoc.TriangleCount >= 1);
            Assert.True(jacketDoc.TriangleCount >= 1);
            // Distinct topology fingerprints (compose may weld verts; triangles must survive).
            Assert.NotEqual(bodyDoc.VertexCount, jacketDoc.VertexCount);

            var session = new ActiveProjectSession();
            var workflow = new ProductWorkflowService(session, new GodProjectArchive(), new AllowlistedAiEditExecutor(session), workRoot: work);
            workflow.NewHumanProject("Compose");
            workflow.SetBodyMeshFromFile(bodyGlb);
            workflow.AddFittedGarment(jacketGlb, "jacket");

            var exported = workflow.ExportActiveGlb(exportPath);
            Assert.Equal(exportPath, exported);
            var doc = CanonicalGltfPipeline.Load(exportPath);
            Assert.True(doc.MeshCount >= 2, $"Expected >=2 meshes in composed export, got {doc.MeshCount}.");
            Assert.True(doc.NodeCount >= 2, $"Expected >=2 nodes, got {doc.NodeCount}.");
            Assert.True(doc.TriangleCount >= bodyDoc.TriangleCount + jacketDoc.TriangleCount,
                $"Expected triangle sum >= {bodyDoc.TriangleCount + jacketDoc.TriangleCount}, got {doc.TriangleCount}.");
            Assert.True(doc.VertexCount >= 8, "Composed export missing geometry.");

            var model = ModelRoot.Load(exportPath);
            var names = model.LogicalNodes.Select(n => n.Name ?? "").Where(n => n.Length > 0).ToList();
            Assert.Contains(names, n => n.Contains("body", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("anny", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(names, n => n.Contains("jacket", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("garment", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("fitted", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task ComposedExport_AfterSaveReopen_StillContainsBodyAndGarment()
    {
        var work = Path.Combine(Path.GetTempPath(), "3dgod-compose-rr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var bodyGlb = Path.Combine(work, "body.glb");
        var jacketGlb = Path.Combine(work, "jacket.glb");
        var projectPath = Path.Combine(work, "scene.3dgod");
        var exportPath = Path.Combine(work, "reopened-export.glb");
        try
        {
            GlbExportService.WriteCompleteSceneStatic(bodyGlb);
            WriteDistinctBoxGlb(jacketGlb, new Vector3(-0.3f, 0.1f, -0.15f), new Vector3(0.3f, 0.45f, 0.15f));

            var session = new ActiveProjectSession();
            var archive = new GodProjectArchive();
            var workflow = new ProductWorkflowService(session, archive, new AllowlistedAiEditExecutor(session), workRoot: work);
            workflow.NewHumanProject("RoundtripCompose");
            workflow.SetBodyMeshFromFile(bodyGlb);
            workflow.AddFittedGarment(jacketGlb, "jacket");
            await workflow.SaveProjectAsync(projectPath);

            var session2 = new ActiveProjectSession();
            var workflow2 = new ProductWorkflowService(session2, archive, new AllowlistedAiEditExecutor(session2), workRoot: work);
            await workflow2.OpenProjectAsync(projectPath);
            Assert.Equal(2, session2.ListSceneParts().Count);

            workflow2.ExportActiveGlb(exportPath);
            var doc = CanonicalGltfPipeline.Load(exportPath);
            Assert.True(doc.MeshCount >= 2);
            Assert.True(doc.TriangleCount >= 2);
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void SceneParts_AfterBodyRegen_MarksGarmentStale()
    {
        var work = Path.Combine(Path.GetTempPath(), "3dgod-stale-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var body = Path.Combine(work, "body.glb");
            var jacket = Path.Combine(work, "j.glb");
            GlbExportService.WriteCompleteSceneStatic(body);
            WriteDistinctBoxGlb(jacket, new Vector3(-0.2f, 0, -0.2f), new Vector3(0.2f, 0.3f, 0.2f));
            var session = new ActiveProjectSession();
            session.NewProject("Stale");
            session.SetActiveMeshFromGlbFile(body, "body");
            session.AddFittedGarment(jacket, "jacket");
            Assert.Equal("fitted", session.Snapshot().GarmentInstances[0].FitState);

            GlbExportService.WriteCompleteSceneStatic(Path.Combine(work, "body2.glb"));
            session.SetActiveMeshFromGlbFile(Path.Combine(work, "body2.glb"), "body");
            Assert.Equal("stale-needs-refit", session.Snapshot().GarmentInstances[0].FitState);
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Headless proof of composed viewport data path: MaterializeSceneGlbs yields body + garment files.
    /// Interactive Helix visual correctness remains IMPLEMENTED_GATED_INTERACTIVE_VISUAL_PROOF.
    /// </summary>
    [Fact]
    public void MaterializeSceneGlbs_BodyPlusGarment_YieldsTwoReadableMeshes()
    {
        var work = Path.Combine(Path.GetTempPath(), "3dgod-viewport-parts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var body = Path.Combine(work, "body.glb");
            var jacket = Path.Combine(work, "jacket.glb");
            GlbExportService.WriteCompleteSceneStatic(body);
            WriteDistinctBoxGlb(jacket, new Vector3(-0.25f, 0.05f, -0.2f), new Vector3(0.25f, 0.4f, 0.2f));

            var session = new ActiveProjectSession();
            session.NewProject("ViewportParts");
            session.SetActiveMeshFromGlbFile(body, "body");
            session.AddFittedGarment(jacket, "jacket");

            var parts = session.MaterializeSceneGlbs(Path.Combine(work, "scene"));
            Assert.Equal(2, parts.Count);
            Assert.Equal("body", parts[0].Role);
            Assert.Equal("garment", parts[1].Role);
            Assert.True(File.Exists(parts[0].GlbPath));
            Assert.True(File.Exists(parts[1].GlbPath));
            Assert.NotEqual(parts[0].GlbPath, parts[1].GlbPath);

            var bodyDoc = CanonicalGltfPipeline.Load(parts[0].GlbPath);
            var garmentDoc = CanonicalGltfPipeline.Load(parts[1].GlbPath);
            Assert.True(bodyDoc.TriangleCount >= 1);
            Assert.True(garmentDoc.TriangleCount >= 1);
            Assert.True(bodyDoc.VertexCount >= 3);
            Assert.True(garmentDoc.VertexCount >= 3);
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task Archive_EmbedsMeshBytes_Roundtrip()
    {
        var path = Path.Combine(Path.GetTempPath(), "3dgod-meshbytes-" + Guid.NewGuid().ToString("N") + ".3dgod");
        var glb = Path.Combine(Path.GetTempPath(), "3dgod-meshbytes-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            GlbExportService.WriteCompleteSceneStatic(glb);
            var bytes = await File.ReadAllBytesAsync(glb);
            var session = new ActiveProjectSession();
            session.NewProject("MeshBytes");
            session.SetActiveMeshBytes(bytes, "body");
            session.UpsertMaterial("skin", 1, 0.8f, 0.7f, 1, 0.2f, 0.5f);

            var archive = new GodProjectArchive();
            await archive.SaveAsync(session.Snapshot(), path);
            var loaded = await archive.LoadAsync(path);

            Assert.NotEmpty(loaded.MeshBytes);
            var meshId = loaded.Meshes[0].MeshAssetId;
            Assert.True(loaded.MeshBytes.ContainsKey(meshId));
            Assert.Equal(bytes.Length, loaded.MeshBytes[meshId].Length);
            Assert.Single(loaded.Materials);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
            if (File.Exists(glb)) File.Delete(glb);
        }
    }

    [Fact]
    public void Composition_RegistersActiveProjectSessionAndWorkflow()
    {
        var services = new ServiceCollection();
        services.AddThreeDGodCoreServices();
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ActiveProjectSession>());
        Assert.NotNull(provider.GetRequiredService<ProductWorkflowService>());
    }

    [Fact]
    public async Task AiEdit_UnsupportedPrompt_DoesNotFakeExecution()
    {
        var session = new ActiveProjectSession();
        session.NewProject("Edit");
        var exec = new AllowlistedAiEditExecutor(session);
        var result = await exec.ExecutePromptAsync("make it nicer");
        Assert.False(result.Ok);
        Assert.Equal("Ambiguous", result.Status);
    }

    /// <summary>
    /// Writes a simple box GLB distinct from WriteCompleteSceneStatic so compose tests can tell meshes apart.
    /// </summary>
    internal static void WriteDistinctBoxGlb(string path, Vector3 min, Vector3 max)
    {
        var positions = new List<Vector3>
        {
            new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z), new(max.X, max.Y, min.Z), new(min.X, max.Y, min.Z),
            new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z), new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z)
        };
        var indices = new List<int>
        {
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6,
            0, 4, 5, 0, 5, 1,
            3, 2, 6, 3, 6, 7,
            0, 3, 7, 0, 7, 4,
            1, 5, 6, 1, 6, 2
        };
        TriangleMeshExport.WriteGlb(path, positions, indices, new Vector4(0.2f, 0.35f, 0.75f, 1f), metallic: 0.05f, roughness: 0.65f);
    }
}

/// <summary>
/// REAL GarmentCode → GarmentFit → session → save → reopen → composed export.
/// Skips with GATED_NOT_INSTALLED when worker missing — never soft-passes.
/// </summary>
[Collection("AnnySerial")]
public class RealClothingPipelineIntegrationTests
{
    [SkippableFact(Timeout = 600000)]
    public async Task RealGarmentCodeFit_SaveReopen_ComposedExport_ContainsBodyAndJacket()
    {
        var probe = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        if (probe.Availability is not (FeatureAvailability.Available or FeatureAvailability.Experimental))
        {
            TestGate.NotInstalled("GarmentCode runtime missing; real clothing pipeline not executed (not PASS_REAL).");
            return;
        }

        var body = Path.Combine(RepoPaths.AssetsDir, "characters", "male_base.glb");
        Assert.True(File.Exists(body), "male_base.glb fixture required for real clothing pipeline.");

        var work = Path.Combine(Path.GetTempPath(), "3dgod-real-cloth-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var projectPath = Path.Combine(work, "clothed.3dgod");
        var exportPath = Path.Combine(work, "clothed-export.glb");
        try
        {
            await using var garment = new GarmentCodeService(new WorkerProcessHost());
            var fit = new GarmentFitService(new AnnyHumanService(new WorkerProcessHost()), garment);
            var result = await fit.FitJacketToBodyAsync(body, work, "male_base");

            Assert.True(File.Exists(result.FittedGlb), "Fitted jacket GLB missing — Fit must run for real.");
            Assert.True(new FileInfo(result.FittedGlb).Length > 512, "Fitted jacket too small.");
            Assert.True(File.Exists(result.ReportPath));
            Assert.Equal(0, result.Report.InsideAfter);
            Assert.Contains("geometry3Sharp", result.Report.Backend ?? "", StringComparison.OrdinalIgnoreCase);

            var fittedDoc = CanonicalGltfPipeline.Load(result.FittedGlb);
            Assert.True(fittedDoc.VertexCount >= 12);
            Assert.True(fittedDoc.TriangleCount >= 1);
            var fittedPos = MeshCompare.ReadPositions(result.FittedGlb);
            foreach (var v in fittedPos)
                Assert.True(float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z));

            var bodyBytes = await File.ReadAllBytesAsync(body);
            var jacketBytes = await File.ReadAllBytesAsync(result.FittedGlb);

            var session = new ActiveProjectSession();
            var archive = new GodProjectArchive();
            var autosave = new AutosaveService(Path.Combine(work, "autosave"), debounce: TimeSpan.FromMilliseconds(5), archive);
            var workflow = new ProductWorkflowService(session, archive, new AllowlistedAiEditExecutor(session), autosave, work);
            workflow.NewHumanProject("ClothedHuman");
            workflow.SetBodyMeshBytes(bodyBytes);
            workflow.AddFittedGarment(result.FittedGlb, "jacket", result.Report);
            Assert.Equal(2, session.ListSceneParts().Count);

            await workflow.FlushAutosaveAsync();
            Assert.NotEmpty(autosave.ListRecoveries());

            await workflow.SaveProjectAsync(projectPath);

            var session2 = new ActiveProjectSession();
            var workflow2 = new ProductWorkflowService(session2, archive, new AllowlistedAiEditExecutor(session2), workRoot: work);
            await workflow2.OpenProjectAsync(projectPath);

            var loaded = session2.Snapshot();
            Assert.Contains(loaded.MeshBytes.Values, b => b.Length == bodyBytes.Length);
            Assert.Contains(loaded.MeshBytes.Values, b => b.Length == jacketBytes.Length);
            Assert.NotEmpty(loaded.GarmentInstances);
            Assert.Equal("fitted", loaded.GarmentInstances[0].FitState);
            Assert.Equal(2, session2.ListSceneParts().Count);

            // Reopen must NOT re-run GarmentCode — export from embedded bytes only.
            workflow2.ExportActiveGlb(exportPath);
            var exported = CanonicalGltfPipeline.Load(exportPath);
            Assert.True(exported.MeshCount >= 2, $"Composed export MeshCount={exported.MeshCount}");
            Assert.True(exported.NodeCount >= 2);
            Assert.True(exported.VertexCount >= 12);
            Assert.True(exported.TriangleCount >= 2);

            var model = ModelRoot.Load(exportPath);
            var names = model.LogicalNodes.Select(n => n.Name ?? "").ToList();
            Assert.Contains(names, n => n.Contains("body", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("male", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(names, n => n.Contains("jacket", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("fitted", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }
}
