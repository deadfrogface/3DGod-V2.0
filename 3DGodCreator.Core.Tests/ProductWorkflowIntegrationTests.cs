using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Export;
using ThreeDGod.Infrastructure;
using ThreeDGod.Persistence;

namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Stage 27: headless product workflow over PRODUCT services (ActiveProjectSession + archive + export).
/// No soft-pass: missing mesh bytes or failed roundtrip fails the test.
/// </summary>
public class ProductWorkflowIntegrationTests
{
    [Fact]
    public async Task HumanWorkflow_New_Edit_Save_Reopen_Export_Survives()
    {
        var recovery = Path.Combine(Path.GetTempPath(), "3dgod-wf-rec-" + Guid.NewGuid().ToString("N"));
        var work = Path.Combine(Path.GetTempPath(), "3dgod-wf-work-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(recovery);
        Directory.CreateDirectory(work);
        var projectPath = Path.Combine(work, "hero.3dgod");
        var exportPath = Path.Combine(work, "hero-export.glb");
        var meshSrc = Path.Combine(work, "body.glb");

        try
        {
            GlbExportService.WriteCompleteSceneStatic(meshSrc);
            Assert.True(File.Exists(meshSrc));
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
            Assert.Equal("Executed", edit.Status);

            // Fake fitted garment mesh (real bytes, not empty) for persistence fidelity
            var garmentGlb = Path.Combine(work, "jacket.glb");
            GlbExportService.WriteCompleteSceneStatic(garmentGlb);
            workflow.AddFittedGarment(garmentGlb, "jacket", new ClippingReport { InsideAfter = 0, MinDistanceAfter = 0.01f });

            await workflow.FlushAutosaveAsync();
            Assert.NotEmpty(autosave.ListRecoveries());

            await workflow.SaveProjectAsync(projectPath);
            Assert.True(File.Exists(projectPath));

            // Close → reopen in a fresh session
            var session2 = new ActiveProjectSession();
            var workflow2 = new ProductWorkflowService(
                session2,
                archive,
                new AllowlistedAiEditExecutor(session2),
                workRoot: work);
            await workflow2.OpenProjectAsync(projectPath);

            var loaded = session2.Snapshot();
            Assert.Equal("Hero", loaded.Project.Name);
            Assert.Single(loaded.Characters);
            Assert.NotNull(loaded.Characters[0].ParametricHumanState);
            Assert.True(loaded.Characters[0].ParametricHumanState!.PhenotypeParameters["height"] > 0.62f);
            Assert.NotEmpty(loaded.MeshBytes);
            Assert.Contains(loaded.MeshBytes.Values, b => b.Length == originalBytes.Length);
            Assert.NotEmpty(loaded.Materials);
            Assert.Equal("skin", loaded.Materials[0].Name);
            Assert.NotEmpty(loaded.GarmentInstances);
            Assert.Equal("fitted", loaded.GarmentInstances[0].FitState);

            var exported = workflow2.ExportActiveGlb(exportPath);
            Assert.Equal(exportPath, exported);
            Assert.True(File.Exists(exportPath));
            var exportedBytes = await File.ReadAllBytesAsync(exportPath);
            Assert.True(exportedBytes.Length > 32);
        }
        finally
        {
            try { Directory.Delete(recovery, true); } catch { /* ignore */ }
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
            Assert.Equal($"assets/{meshId:D}/mesh.glb", loaded.Meshes[0].CanonicalGlbPath);
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
        Assert.NotNull(provider.GetRequiredService<AutosaveService>());
        Assert.NotNull(provider.GetRequiredService<AllowlistedAiEditExecutor>());
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
}
