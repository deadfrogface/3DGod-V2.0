using System.Numerics;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Domain;
using ThreeDGod.Export;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Rigging;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

public class PipelineBreadcrumbTests
{
    [Fact]
    public async Task ProjectSaveLoad_EmitsRealBreadcrumbs_AndFailureCapturesFailingStage()
    {
        var diagnostics = new DiagnosticService();
        var archive = new GodProjectArchive(diagnostics);
        var path = Path.Combine(Path.GetTempPath(), $"3dgod-bc-{Guid.NewGuid():N}.3dgod");
        var bundle = new ProjectBundle
        {
            Project = new ProjectDocument { Name = "breadcrumb" },
            Characters = [new CharacterDocument { Name = "c1" }]
        };
        try
        {
            await archive.SaveAsync(bundle, path);
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Project.Save" && b.Status == "Completed");

            diagnostics.ClearBreadcrumbs();
            await archive.LoadAsync(path);
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Project.Load" && b.Status == "Completed");

            diagnostics.ClearBreadcrumbs();
            var missing = path + ".missing";
            var ex = await Assert.ThrowsAsync<ProjectArchiveException>(() => archive.LoadAsync(missing));
            var issue = diagnostics.Capture(ex, "Project.Load");
            Assert.Equal("Project", issue.Pipeline);
            Assert.Equal("Project.Load", issue.FailingStage);
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Project.Load" && b.Status == "Failed");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }
    }

    [Fact]
    public void ImportAssimpMissing_EmitsFailedImportParse_FromRealGate()
    {
        var diagnostics = new DiagnosticService();
        var fake = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".fbx");
        var ex = Assert.Throws<InvalidOperationException>(() => AssimpImportGate.Import(fake, diagnostics));
        Assert.Contains("NotInstalled", ex.Message, StringComparison.Ordinal);
        Assert.Contains(diagnostics.Breadcrumbs, b => b.Pipeline == "Import" && b.Stage == "Import.Parse" && b.Status == "Failed" && b.Provider == "assimp");
        var issue = diagnostics.Capture(ex, "Import.Parse");
        Assert.Equal("Import", issue.Pipeline);
        Assert.Equal("Import.Parse", issue.FailingStage);
        Assert.Equal("assimp", diagnostics.Breadcrumbs.Last(b => b.Status == "Failed").Provider);
    }

    [Fact]
    public void ImportObj_EmitsParseValidateAndMeshValidate_FromRealImporter()
    {
        var diagnostics = new DiagnosticService();
        var obj = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".obj");
        File.WriteAllText(obj, "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");
        try
        {
            var mesh = AssimpImportGate.Import(obj, diagnostics);
            Assert.Equal(3, mesh.Positions.Count);
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Import.Parse" && b.Status == "Completed");
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Import.Validate" && b.Status == "Completed");
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Mesh.Validate" && b.Status == "Completed");
        }
        finally
        {
            if (File.Exists(obj)) File.Delete(obj);
        }
    }

    [Fact]
    public void RemeshService_EmitsMeshCleanupUvLod_FromRealPipeline()
    {
        var diagnostics = new DiagnosticService();
        var svc = new RemeshService(diagnostics);
        var src = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-src.glb");
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-dst.glb");
        try
        {
            var b = new MeshBuilder3D();
            b.AddSphere(Vector3.Zero, 0.5f, 16, 12);
            TriangleMeshExport.WriteGlb(src, b.Positions, b.Indices);
            svc.RemeshGlb(src, dst, RemeshProfile.Preview);
            Assert.True(File.Exists(dst));
            Assert.Contains(diagnostics.Breadcrumbs, c => c.Stage == "Mesh.Cleanup" && c.Status == "Completed");
            Assert.Contains(diagnostics.Breadcrumbs, c => c.Stage == "Mesh.Validate");
            Assert.Contains(diagnostics.Breadcrumbs, c => c.Stage == "Mesh.UV" && c.Status == "Completed");
            Assert.Contains(diagnostics.Breadcrumbs, c => c.Stage == "Mesh.LOD" && c.Status == "Completed");
        }
        finally
        {
            if (File.Exists(src)) File.Delete(src);
            if (File.Exists(dst)) File.Delete(dst);
        }
    }

    [Fact]
    public void GlbExport_EmitsPreflightAndWrite_FromRealExporter()
    {
        var diagnostics = new DiagnosticService();
        var src = Path.Combine(RepoPaths.AssetsDir, "characters", "male_base.glb");
        var dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            GlbExportService.Export(src, dst, diagnostics);
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Export.Preflight" && b.Status == "Completed");
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Export.Write" && b.Status == "Completed");
        }
        finally
        {
            if (File.Exists(dst)) File.Delete(dst);
        }
    }

    [Fact]
    public async Task FreeformPipeline_EmitsAiFallbackMeshAndRig_FromRealCode()
    {
        var diagnostics = new DiagnosticService();
        var images = new ReferenceImageService(diagnostics);
        var to3d = new ImageTo3DService(diagnostics);
        var pipeline = new FreeformPipeline(images, to3d, diagnostics);
        var root = Path.Combine(Path.GetTempPath(), "ff-bc-" + Guid.NewGuid().ToString("N"));
        try
        {
            var bundle = new ProjectBundle();
            await pipeline.RunAsync("kleiner drache", bundle, root);
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "ReferenceImage.Generate" && b.Status is "Failed" or "Fallback");
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "ImageTo3D.Generate" && b.Status == "Fallback");
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Mesh.Cleanup" && b.Status == "Completed");
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Rig.Skeleton" && b.Status == "Completed");
            var lastOk = diagnostics.Breadcrumbs.Last(b => b.Status is "Completed" or "Fallback");
            Assert.False(string.IsNullOrWhiteSpace(lastOk.Pipeline));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ExportPreflight_EmitsBreadcrumb_FromRealSanityCheck()
    {
        var diagnostics = new DiagnosticService();
        var issues = ExportPreflight.FbxSanity(null, diagnostics);
        Assert.NotEmpty(issues);
        Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Export.Preflight" && b.Status == "Completed");
    }

    [Fact]
    public void RigWeightTransfer_EmitsBreadcrumb_FromRealSkinBinder()
    {
        var diagnostics = new DiagnosticService();
        var skin = new GarmentSkinService(diagnostics);
        var body = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-body.glb");
        var garment = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-g.glb");
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-bound.glb");
        try
        {
            HumanoidTestRig.WriteGood(body);
            var b = new MeshBuilder3D();
            b.AddSphere(new Vector3(0, 1.2f, 0), 0.08f, 8, 6);
            TriangleMeshExport.WriteGlb(garment, b.Positions, b.Indices);
            skin.BindToBody(body, garment, dest);
            Assert.Contains(diagnostics.Breadcrumbs, c => c.Stage == "Rig.WeightTransfer" && c.Status == "Completed");
        }
        finally
        {
            foreach (var f in new[] { body, garment, dest })
                if (File.Exists(f)) File.Delete(f);
        }
    }

    [Fact]
    public async Task HumanGenerate_EmitsBreadcrumb_FromRealAnnyService()
    {
        var diagnostics = new DiagnosticService();
        await using var anny = new AnnyHumanService(new WorkerProcessHost(diagnostics), diagnostics);
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-human.glb");
        try
        {
            try
            {
                await anny.GenerateGlbAsync(dest);
            }
            catch (InvalidOperationException)
            {
                // Gated or worker failure still must leave Human.Generate breadcrumbs from the real service path.
            }

            Assert.Contains(diagnostics.Breadcrumbs,
                b => b.Pipeline == "Human" && b.Stage == "Human.Generate" && b.Status is "Started" or "Completed" or "Failed");
            Assert.Contains(diagnostics.Breadcrumbs,
                b => b.Stage == "Human.Generate" && b.Status is "Completed" or "Failed" && b.Provider == "anny");
        }
        finally
        {
            if (File.Exists(dest)) File.Delete(dest);
            var obj = Path.ChangeExtension(dest, ".obj");
            if (File.Exists(obj)) File.Delete(obj);
        }
    }

    [Fact]
    public async Task GarmentGenerate_EmitsBreadcrumb_FromRealGarmentCodeService()
    {
        var diagnostics = new DiagnosticService();
        await using var garment = new GarmentCodeService(new WorkerProcessHost(diagnostics), diagnostics);
        var dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-jacket.glb");
        try
        {
            try
            {
                await garment.GenerateJacketGlbAsync(dest);
            }
            catch (InvalidOperationException)
            {
            }

            Assert.Contains(diagnostics.Breadcrumbs,
                b => b.Pipeline == "Garment" && b.Stage == "Garment.Generate" && b.Status is "Completed" or "Failed");
            Assert.Contains(diagnostics.Breadcrumbs,
                b => b.Stage == "Garment.Generate" && b.Provider == "garmentcode");
        }
        finally
        {
            if (File.Exists(dest)) File.Delete(dest);
            var obj = Path.ChangeExtension(dest, ".obj");
            if (File.Exists(obj)) File.Delete(obj);
        }
    }

    [Fact]
    public async Task GarmentFit_EmitsBreadcrumb_FromRealFitService()
    {
        var diagnostics = new DiagnosticService();
        await using var anny = new AnnyHumanService(new WorkerProcessHost(diagnostics), diagnostics);
        await using var garment = new GarmentCodeService(new WorkerProcessHost(diagnostics), diagnostics);
        var fit = new GarmentFitService(anny, garment, diagnostics);
        var root = Path.Combine(Path.GetTempPath(), "fit-bc-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            var body = Path.Combine(root, "body.glb");
            HumanoidTestRig.WriteGood(body);
            try
            {
                await fit.FitJacketToBodyAsync(body, root, "breadcrumb-fit");
            }
            catch (InvalidOperationException)
            {
            }

            Assert.Contains(diagnostics.Breadcrumbs,
                b => b.Pipeline == "Garment" && b.Stage == "Garment.Fit" && b.Status is "Completed" or "Failed");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void DiagnosticCapture_ExposesPipelineLastOkFailingStageAndProvider()
    {
        var diagnostics = new DiagnosticService();
        PipelineTrace.Stage(diagnostics, "Import", "Import.Parse", "Started", "obj");
        PipelineTrace.Stage(diagnostics, "Import", "Import.Parse", "Completed", "obj");
        PipelineTrace.Stage(diagnostics, "Import", "Import.Validate", "Started", "obj");
        PipelineTrace.Stage(diagnostics, "Import", "Import.Validate", "Failed", "obj");
        var issue = diagnostics.Capture(new InvalidOperationException("validate failed"), "Import.Validate");
        Assert.Equal("Import", issue.Pipeline);
        Assert.Equal("Import.Validate", issue.FailingStage);
        Assert.Equal("Import.Parse", issue.LastSuccessfulStage);
        Assert.Equal("obj", diagnostics.Breadcrumbs.Last(b => b.Status == "Failed").Provider);
        Assert.NotEmpty(issue.Breadcrumbs);
    }
}
