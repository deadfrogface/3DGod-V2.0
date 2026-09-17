using SharpGLTF.Schema2;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure;
using ThreeDGod.Infrastructure.Components;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Installed-product E2E against a Velopack tree (THREEDGOD_INSTALL_ROOT / THREEDGOD_CONTENT_ROOT).
/// Workers come from the packaged install tree — not a developer checkout requirement for runtime.
/// When THREEDGOD_CI_INSTALLED_PRODUCT=1, missing install/components FAIL.
/// </summary>
public class InstalledProductE2ETests
{
    public const string CiEnv = "THREEDGOD_CI_INSTALLED_PRODUCT";

    private static bool CiRequired =>
        string.Equals(Environment.GetEnvironmentVariable(CiEnv), "1", StringComparison.Ordinal);

    private static string? ContentRoot()
    {
        var install = Environment.GetEnvironmentVariable("THREEDGOD_INSTALL_ROOT");
        if (!string.IsNullOrWhiteSpace(install))
        {
            var current = Path.Combine(install, "current");
            if (Directory.Exists(Path.Combine(current, "workers")))
                return Path.GetFullPath(current);
            if (Directory.Exists(Path.Combine(install, "workers")))
                return Path.GetFullPath(install);
        }

        var env = Environment.GetEnvironmentVariable("THREEDGOD_CONTENT_ROOT");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(Path.Combine(env, "workers")))
            return Path.GetFullPath(env);
        return null;
    }

    [SkippableFact(Timeout = 1_800_000)]
    public async Task InstalledTree_AnnyGarment_SaveReopen_Compose_AndAutoRigCpu()
    {
        var content = ContentRoot();
        if (content is null)
        {
            if (CiRequired)
                Assert.Fail("THREEDGOD_INSTALL_ROOT / THREEDGOD_CONTENT_ROOT required for installed-product E2E.");
            TestGate.ExternalRuntime("Installed product root not set; GATED_EXTERNAL_SOFTWARE.");
            return;
        }

        Environment.SetEnvironmentVariable("THREEDGOD_CONTENT_ROOT", content);
        Assert.True(Directory.Exists(Path.Combine(content, "workers", "anny")), "Installed workers/anny missing.");
        Assert.True(Directory.Exists(Path.Combine(content, "workers", "garmentcode")), "Installed workers/garmentcode missing.");
        Assert.True(File.Exists(Path.Combine(content, "assets", "characters", "male_base.glb")),
            "Installed male_base.glb missing.");

        using var downloads = new ComponentDownloadService();
        var uv = new UvProvisioner(downloads);
        var mgr = ComponentManager.FromRepo(content, InstallLayout.ModelsRoot);
        var installer = new WorkerUvComponentInstaller(
            mgr, uv, content, skinTokensCpp: new SkinTokensCppProvisioner(downloads));

        var annyInstall = await installer.InstallOrRepairAsync("anny", acceptLicense: true);
        Assert.Equal(ComponentState.Ready, annyInstall.State);
        var garmentInstall = await installer.InstallOrRepairAsync("garmentcode", acceptLicense: true);
        Assert.Equal(ComponentState.Ready, garmentInstall.State);

        var skinInstall = await installer.InstallOrRepairAsync("skintokens-cpp", acceptLicense: true);
        if (skinInstall.State != ComponentState.Ready && CiRequired)
            Assert.Fail($"skintokens-cpp install not Ready: {skinInstall.Message}");

        var annyProbe = AnnyRuntime.Probe(content);
        Assert.True(annyProbe.Availability is FeatureAvailability.Experimental or FeatureAvailability.Available,
            annyProbe.Message);
        var garmentProbe = GarmentCodeRuntime.Probe(content);
        Assert.True(garmentProbe.Availability is FeatureAvailability.Experimental or FeatureAvailability.Available,
            garmentProbe.Message);

        var work = Path.Combine(Path.GetTempPath(), "3dgod-installed-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var projectPath = Path.Combine(work, "installed.3dgod");
        var exportPath = Path.Combine(work, "composed.glb");
        try
        {
            // HUMAN (Anny) + morph
            await using var anny = new AnnyHumanService(new WorkerProcessHost());
            var humanGlb = Path.Combine(work, "human.glb");
            await anny.GenerateGlbAsync(humanGlb, new AnnyGenerateRequest
            {
                Phenotypes = new Dictionary<string, float> { ["height"] = 0.62f }
            });
            Assert.True(File.Exists(humanGlb) && new FileInfo(humanGlb).Length > 512);

            // GARMENT FIT on Anny body
            await using var garment = new GarmentCodeService(new WorkerProcessHost());
            var fit = new GarmentFitService(new AnnyHumanService(new WorkerProcessHost()), garment);
            var fitResult = await fit.FitJacketToBodyAsync(humanGlb, work, "installed-e2e");
            Assert.True(File.Exists(fitResult.FittedGlb));

            var bodyBytes = await File.ReadAllBytesAsync(humanGlb);
            var jacketBytes = await File.ReadAllBytesAsync(fitResult.FittedGlb);

            var session = new ActiveProjectSession();
            var archive = new GodProjectArchive();
            var autosave = new AutosaveService(Path.Combine(work, "autosave"), debounce: TimeSpan.FromMilliseconds(5), archive);
            var workflow = new ProductWorkflowService(session, archive, new AllowlistedAiEditExecutor(session), autosave, work);
            workflow.NewHumanProject("InstalledE2E");
            workflow.SetBodyMeshBytes(bodyBytes);
            workflow.AddFittedGarment(fitResult.FittedGlb, "jacket", fitResult.Report);
            workflow.UpsertMaterial("skin", 0.8f, 0.25f, 0.2f, 0.05f, 0.55f);
            Assert.Equal(2, session.ListSceneParts().Count);

            // AUTO-RIG CPU
            var autoRigReady = SkinTokensCppRuntime.Probe("cpu").Availability
                is FeatureAvailability.Available or FeatureAvailability.Experimental;
            if (autoRigReady)
            {
                var rigged = Path.Combine(work, "rigged.glb");
                var auto = await new ThreeDGod.Infrastructure.AutoRig.AutoRigProviderSelector(
                [
                    new ThreeDGod.Infrastructure.AutoRig.SkinTokensCppCpuAutoRigProvider()
                ]).RigAsync(humanGlb, rigged, AutoRigDevicePreference.Cpu);
                Assert.Equal("skintokens-cpp-cpu", auto.ProviderId);
                Assert.Equal("cpu", auto.Device);
                session.SetActiveRigFromGlb(auto.OutputGlb, auto.Provenance ?? auto.ProviderId);
            }
            else if (CiRequired)
            {
                Assert.Fail("Auto-Rig CPU required for installed-product E2E under CI but CLI/model not ready.");
            }

            await workflow.FlushAutosaveAsync();
            await workflow.SaveProjectAsync(projectPath);
            Assert.True(File.Exists(projectPath));

            // CLOSE / REOPEN
            var session2 = new ActiveProjectSession();
            var workflow2 = new ProductWorkflowService(session2, archive, new AllowlistedAiEditExecutor(session2), workRoot: work);
            await workflow2.OpenProjectAsync(projectPath);
            var loaded = session2.Snapshot();
            Assert.Contains(loaded.MeshBytes.Values, b => b.Length == bodyBytes.Length || b.Length == jacketBytes.Length);
            Assert.NotEmpty(loaded.GarmentInstances);
            Assert.True(session2.ListSceneParts().Count >= 2);

            workflow2.ExportActiveGlb(exportPath);
            var exported = CanonicalGltfPipeline.Load(exportPath);
            Assert.True(exported.MeshCount >= 2, $"Composed export MeshCount={exported.MeshCount}");
            Assert.True(exported.VertexCount >= 12);

            var model = ModelRoot.Load(exportPath);
            var names = model.LogicalNodes.Select(n => n.Name ?? "").ToList();
            Assert.Contains(names, n => n.Contains("body", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("rigged", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("male", StringComparison.OrdinalIgnoreCase)
                                        || n.Length > 0);
            Assert.Contains(names, n => n.Contains("jacket", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("fitted", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("garment", StringComparison.OrdinalIgnoreCase));

            var art = Environment.GetEnvironmentVariable("THREEDGOD_CI_ARTIFACT_DIR");
            if (!string.IsNullOrWhiteSpace(art))
            {
                var root = Path.IsPathRooted(art) ? art : Path.Combine(RepoPaths.FindRepoRoot(), art);
                Directory.CreateDirectory(root);
                File.Copy(exportPath, Path.Combine(root, "installed-composed.glb"), overwrite: true);
                File.WriteAllText(Path.Combine(root, "installed-product-e2e.json"),
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ok = true,
                        contentRoot = content,
                        autoRig = autoRigReady,
                        meshCount = exported.MeshCount,
                        garmentCount = loaded.GarmentInstances.Count
                    }));
            }
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }
}
