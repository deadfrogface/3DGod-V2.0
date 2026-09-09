using ThreeDGod.Infrastructure;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class LegacyBlenderBackendTests
{
    [Fact]
    public void MissingRuntime_IsUnavailable_AndDoesNotThrow()
    {
        var backend = new LegacyBlenderBackend(new ConfigService());
        var configured = backend.IsBlenderConfigured();
        var launched = backend.TryRunHeadlessJob(
            Path.Combine(RepoPaths.FindRepoRoot(), "blender_embed", "blender_runtime_test.py"),
            out _,
            out var error);

        var cs = new CharacterSystem(new ConfigService(), backend, new PresetService());
        Assert.NotNull(cs.SculptData);

        if (!configured)
        {
            Assert.False(launched);
            Assert.Contains("unavailable", error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public void HeadlessSmoke_WhenRuntimePresent_PrintsOk()
    {
        var backend = new LegacyBlenderBackend(new ConfigService());
        if (!backend.IsBlenderConfigured())
        {
            TestGate.NotInstalled("Blender runtime missing; headless smoke not executed.");
            return;
        }

        var script = Path.Combine(RepoPaths.FindRepoRoot(), "blender_embed", "blender_runtime_test.py");
        Assert.True(File.Exists(script));
        var ok = backend.TryRunHeadlessJob(script, out var output, out var error);
        Assert.True(ok, error);
        Assert.Contains("BLENDER_RUNTIME_OK", output, StringComparison.Ordinal);
    }

    [Fact]
    public void LaunchMethods_UseHeadlessFlagsInSource()
    {
        var path = Path.Combine(RepoPaths.FindRepoRoot(), "src", "ThreeDGod.Infrastructure", "LegacyBlenderBackend.cs");
        var src = File.ReadAllText(path);
        Assert.Contains("CreateNoWindow = true", src, StringComparison.Ordinal);
        Assert.Contains("--background", src, StringComparison.Ordinal);
        Assert.DoesNotContain("keepAlive: true", src, StringComparison.Ordinal);
    }

    [Fact]
    public void LaunchAutoRig_DoesNotDelegateToSculpt_AndReportsNotImplemented()
    {
        var path = Path.Combine(RepoPaths.FindRepoRoot(), "src", "ThreeDGod.Infrastructure", "LegacyBlenderBackend.cs");
        var src = File.ReadAllText(path);
        Assert.DoesNotContain("LaunchAutoRig() => LaunchSculpt()", src, StringComparison.Ordinal);
        Assert.Contains("LaunchAutoRig()", src, StringComparison.Ordinal);
        Assert.Contains("NotImplemented – Auto-Rig", src, StringComparison.Ordinal);

        var backend = new LegacyBlenderBackend(new ConfigService());
        BlenderErrorInfo? seen = null;
        backend.OnBlenderFailed += info => seen = info;
        backend.LaunchAutoRig();
        Assert.NotNull(seen);
        Assert.Contains("NotImplemented", seen!.Message, StringComparison.Ordinal);
        Assert.Contains("Sculpt will not be started", seen.Message, StringComparison.OrdinalIgnoreCase);
    }
}
