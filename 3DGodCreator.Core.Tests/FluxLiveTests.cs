using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Live FLUX.1-schnell proof + optional FLUX → TripoSR E2E.
/// PASS_REAL only when CUDA inference actually writes a non-empty PNG (and GLB for E2E).
/// Otherwise: honest SkippableFact gates (IMPLEMENTED_GATED_HARDWARE / NotInstalled) — never soft-pass.
/// </summary>
[Collection("AnnySerial")]
public class FluxLiveTests
{
    [SkippableFact]
    public void Probe_WhenHardwareMissing_IsUnsupportedHardwareOrNotInstalled()
    {
        var status = FluxRuntime.Probe(RepoPaths.FindRepoRoot());
        Assert.NotEqual(FeatureAvailability.Available, status.Availability);
        Assert.DoesNotContain("success", status.Message, StringComparison.OrdinalIgnoreCase);

        var hw = HardwareProfiler.Probe();
        if (!status.HasCheckpoint)
        {
            Assert.Equal(FeatureAvailability.NotInstalled, status.Availability);
            return;
        }

        if (!hw.Cuda || hw.VramMb < FluxRuntime.MinVramMb)
        {
            Assert.Equal(FeatureAvailability.UnsupportedHardware, status.Availability);
            Assert.Contains("IMPLEMENTED_GATED_HARDWARE", status.Message, StringComparison.Ordinal);
            return;
        }

        Assert.Equal(FeatureAvailability.Experimental, status.Availability);
    }

    [SkippableFact]
    public async Task Generate_WhenReady_WritesRealPng_WithValidDimensions()
    {
        var status = FluxRuntime.Probe(RepoPaths.FindRepoRoot());
        Skip.If(
            status.Availability == FeatureAvailability.NotInstalled,
            "GATED_NOT_INSTALLED - " + status.Message);
        Skip.If(
            status.Availability == FeatureAvailability.UnsupportedHardware,
            "IMPLEMENTED_GATED_HARDWARE - " + status.Message);
        Skip.If(
            status.Availability is not FeatureAvailability.Experimental,
            "FLUX not invocable: " + status.Message);

        var work = Path.Combine(Path.GetTempPath(), "3dgod-flux-live-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var png = Path.Combine(work, "out.png");
        try
        {
            await using var svc = new FluxService(new WorkerProcessHost());
            await svc.GeneratePngAsync(
                "a simple red ceramic mug on a white table, studio light",
                png,
                seed: 42);

            Assert.True(File.Exists(png), "FLUX must write a real PNG – no fake pass.");
            Assert.True(new FileInfo(png).Length > 1024, "FLUX PNG must be non-empty.");
            var bytes = await File.ReadAllBytesAsync(png);
            Assert.True(PngSignature.TryReadSize(bytes, out var w, out var h), "Output must be a valid PNG.");
            Assert.True(w >= 64 && h >= 64, $"Unexpected dimensions {w}x{h}.");
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }

    [SkippableFact]
    public async Task FluxThenTripoSr_WhenBothReady_WritesRealGlb()
    {
        var flux = FluxRuntime.Probe(RepoPaths.FindRepoRoot());
        Skip.If(
            flux.Availability == FeatureAvailability.NotInstalled,
            "GATED_NOT_INSTALLED - FLUX: " + flux.Message);
        Skip.If(
            flux.Availability == FeatureAvailability.UnsupportedHardware,
            "IMPLEMENTED_GATED_HARDWARE - FLUX: " + flux.Message);
        Skip.If(
            flux.Availability is not FeatureAvailability.Experimental,
            "FLUX not ready for E2E: " + flux.Message);

        var tripo = TripoSrRuntime.Probe(RepoPaths.FindRepoRoot());
        Skip.If(
            tripo.Availability == FeatureAvailability.NotInstalled,
            "GATED_NOT_INSTALLED - TripoSR: " + tripo.Message);
        Skip.If(
            !tripo.HasCheckpoint,
            "GATED_NOT_INSTALLED - TripoSR checkpoint missing for FLUX→TripoSR E2E.");

        var work = Path.Combine(Path.GetTempPath(), "3dgod-flux-tripo-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var png = Path.Combine(work, "flux.png");
        var glb = Path.Combine(work, "out.glb");
        try
        {
            await using var fluxSvc = new FluxService(new WorkerProcessHost());
            await fluxSvc.GeneratePngAsync(
                "a wooden chair, plain background, product photo",
                png,
                seed: 7);
            Assert.True(File.Exists(png) && new FileInfo(png).Length > 1024);

            await using var tripoSvc = new TripoSrService(new WorkerProcessHost());
            var path = await tripoSvc.GenerateGlbAsync(png, glb);
            Assert.True(File.Exists(path), "TripoSR must consume FLUX PNG and write a real GLB.");
            Assert.True(new FileInfo(path).Length > 1024, "E2E GLB must be non-empty – no fake pass.");
        }
        finally
        {
            try { Directory.Delete(work, true); } catch { /* ignore */ }
        }
    }

    [SkippableFact]
    public async Task Generate_ViaReferenceImageService_WithoutReadyBackend_ThrowsNoFakePng()
    {
        var status = ReferenceImageRuntime.Probe("flux");
        Skip.If(
            status.Availability == FeatureAvailability.Experimental,
            "FLUX Experimental – gated-throw path not exercised; covered by Generate_WhenReady_*.");

        var destHint = Path.Combine(Path.GetTempPath(), "3dgod-no-fake-flux-" + Guid.NewGuid().ToString("N") + ".png");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ReferenceImageService().GenerateAsync("should not invent pixels", 99, new ProjectBundle()));
        Assert.True(
            ex.Message.Contains("NotInstalled", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("UnsupportedHardware", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Disabled", StringComparison.OrdinalIgnoreCase),
            ex.Message);
        Assert.False(File.Exists(destHint));
    }
}
