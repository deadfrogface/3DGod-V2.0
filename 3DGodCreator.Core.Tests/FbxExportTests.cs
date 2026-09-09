using Microsoft.Extensions.DependencyInjection;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Export;
using ThreeDGod.Infrastructure;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class FbxExportTests
{
    private static FbxExportService CreateService(LegacyBlenderBackend backend, IDiagnosticService? diagnostics = null) =>
        new(
            () => backend.IsBlenderConfigured(),
            (src, dst) => backend.TryExportGlbToFbx(src, dst, out var err) ? null : err,
            diagnostics);

    [Fact]
    public void WhenBlenderMissing_ThrowsGatedNotInstalled()
    {
        var backend = new LegacyBlenderBackend(new ConfigService());
        if (backend.IsBlenderConfigured())
        {
            TestGate.NotInstalled("Blender present on CI/dev machine – skip missing-runtime assertion.");
            return;
        }

        var svc = CreateService(backend);
        var glb = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".glb");
        var fbx = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".fbx");
        try
        {
            GlbExportService.WriteCompleteSceneStatic(glb);
            var ex = Assert.Throws<InvalidOperationException>(() => svc.Export(glb, fbx));
            Assert.Contains("GATED_NOT_INSTALLED", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(glb)) File.Delete(glb);
            if (File.Exists(fbx)) File.Delete(fbx);
        }
    }

    [Fact]
    public void WhenBlenderPresent_ExportsCompleteSceneGlbToFbx()
    {
        var backend = new LegacyBlenderBackend(new ConfigService());
        if (!backend.IsBlenderConfigured())
        {
            TestGate.NotInstalled("Blender runtime missing; live GLB→FBX export not executed.");
            return;
        }

        var root = Path.Combine(Path.GetTempPath(), "3dgod-fbx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var glb = Path.Combine(root, "scene.glb");
        var fbx = Path.Combine(root, "scene.fbx");
        try
        {
            GlbExportService.WriteCompleteSceneStatic(glb);
            var diagnostics = new DiagnosticService();
            var svc = CreateService(backend, diagnostics);
            svc.Export(glb, fbx, "scene", runUe5Preflight: true);

            Assert.True(File.Exists(fbx));
            var sanity = FbxSanity.Check(fbx);
            Assert.True(sanity.Passed, string.Join("; ", sanity.Issues));
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Export.Write" && b.Provider == "blender-fbx");
            Assert.Contains(diagnostics.Breadcrumbs, b => b.Stage == "Export.Preflight" && b.Provider == "ue5-preflight");
            Assert.DoesNotContain("UE5 editor import success", string.Join(" ", sanity.Issues), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void FbxSanity_DetectsKaydaraBinaryHeader_WithoutBlender()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".fbx");
        try
        {
            var payload = new byte[FbxSanity.MinimumBytes + 32];
            "Kaydara FBX Binary"u8.CopyTo(payload);
            File.WriteAllBytes(path, payload);

            Assert.True(FbxSanity.TryReadKaydaraHeader(path, out var kind));
            Assert.Equal("Kaydara binary", kind);
            var sanity = FbxSanity.Check(path);
            Assert.True(sanity.Passed);
            Assert.True(sanity.HasKaydaraHeader);
            Assert.Contains(sanity.Issues, i => i.Contains("UE5 editor import not claimed", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void FbxSanity_MissingFile_DoesNotClaimUe5Import()
    {
        var sanity = FbxSanity.Check(null);
        Assert.False(sanity.Passed);
        Assert.Contains(sanity.Issues, i => i.Contains("UE5", StringComparison.Ordinal));
    }

    [Fact]
    public void Di_RegistersFbxExportService()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddThreeDGodCoreServices();
        var provider = services.BuildServiceProvider();
        var svc = provider.GetRequiredService<IFbxExportService>();
        Assert.IsType<FbxExportService>(svc);
    }
}
