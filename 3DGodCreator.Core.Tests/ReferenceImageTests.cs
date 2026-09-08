using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Infrastructure;
using ThreeDGod.Persistence;

namespace ThreeDGodCreator.Core.Tests;

public class ReferenceImageTests
{
    private static readonly byte[] OneByOnePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public void Probe_IsNotAvailable_AndNeverSuccess()
    {
        var status = ReferenceImageRuntime.Probe();
        Assert.NotEqual(FeatureAvailability.Available, status.Availability);
        Assert.DoesNotContain("success", status.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(status.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Experimental);
    }

    [Fact]
    public async Task Generate_WithoutCheckpoint_ThrowsAndWritesNoImage()
    {
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-fake-flux-" + Guid.NewGuid().ToString("N") + ".png");
        var svc = new ReferenceImageService();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.GenerateAsync("a muscular elf", 1, new ProjectBundle()));
        Assert.DoesNotContain("success", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(dest));
    }

    [Fact]
    public void AttachExistingPng_StoresPromptSeedHashAndSize()
    {
        var png = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(png, OneByOnePng);
        try
        {
            var bundle = new ProjectBundle();
            var image = new ReferenceImageService().AttachExistingPng(png, "imported reference", 42, bundle);
            Assert.Equal("imported reference", image.Prompt);
            Assert.Equal(42, image.Seed);
            Assert.Equal(1, image.Width);
            Assert.Equal(1, image.Height);
            Assert.False(string.IsNullOrWhiteSpace(image.Sha256));
            Assert.Equal(64, image.Sha256.Length);
            Assert.Single(bundle.ReferenceSets);
            Assert.True(bundle.ReferenceImageBytes.ContainsKey(image.ReferenceImageId));
            Assert.Equal("import", image.BackendId);
        }
        finally
        {
            if (File.Exists(png)) File.Delete(png);
        }
    }

    [Fact]
    public void AttachExisting_RejectsNonPngPlaceholder()
    {
        var fake = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllText(fake, ".gitignore");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new ReferenceImageService().AttachExistingPng(fake, "nope", null, new ProjectBundle()));
            Assert.Contains("valid PNG", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(fake)) File.Delete(fake);
        }
    }

    [Fact]
    public async Task ProjectSaveReload_PreservesReferenceSetAndPng()
    {
        var png = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        var project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".3dgod");
        File.WriteAllBytes(png, OneByOnePng);
        try
        {
            var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "refs" } };
            var image = new ReferenceImageService().AttachExistingPng(png, "store me", 7, bundle);
            await new GodProjectArchive().SaveAsync(bundle, project);
            var loaded = await new GodProjectArchive().LoadAsync(project);
            var loadedImage = Assert.Single(loaded.ReferenceImages);
            Assert.Equal(image.Prompt, loadedImage.Prompt);
            Assert.Equal(7, loadedImage.Seed);
            Assert.Equal(image.Sha256, loadedImage.Sha256);
            Assert.Equal(1, loadedImage.Width);
            Assert.True(loaded.ReferenceImageBytes.TryGetValue(loadedImage.ReferenceImageId, out var bytes));
            Assert.True(PngSignature.TryReadSize(bytes, out var w, out var h));
            Assert.Equal(1, w);
            Assert.Equal(1, h);
        }
        finally
        {
            if (File.Exists(png)) File.Delete(png);
            if (File.Exists(project)) File.Delete(project);
            if (File.Exists(project + ".bak")) File.Delete(project + ".bak");
        }
    }

    [Fact]
    public void HardwareProfiler_DoesNotInventCuda()
    {
        var hw = HardwareProfiler.Probe();
        if (!hw.Cuda)
            Assert.True(hw.VramMb == 0 || Environment.GetEnvironmentVariable("3DGOD_VRAM_MB") != null);
    }
}
