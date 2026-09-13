using ThreeDGod.Application;
using ThreeDGod.Mesh;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Live CPU integration for CI. When THREEDGOD_CI_RUNTIME_INTEGRATION=1, missing deps FAIL (no soft skip).
/// When unset, tests are explicitly GATED_EXTERNAL_RUNTIME (not PASS).
/// </summary>
public class CiRuntimeIntegrationTests
{
    public const string EnvFlag = "THREEDGOD_CI_RUNTIME_INTEGRATION";

    private static bool IntegrationRequested =>
        string.Equals(Environment.GetEnvironmentVariable(EnvFlag), "1", StringComparison.Ordinal);

    private static void RequireIntegrationOrGate(string capability)
    {
        if (!IntegrationRequested)
            TestGate.ExternalRuntime($"{capability}: set {EnvFlag}=1 after uv sync to run live CI integration (not PASS).");
    }

    [SkippableFact(Timeout = 900000)]
    public async Task Anny_CpuGenerate_WritesRealGlb()
    {
        RequireIntegrationOrGate("Anny");

        var probe = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        Assert.False(
            probe.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled,
            "Anny runtime missing while CI integration requested. Blocker: " + probe.Message);

        var dest = Path.Combine(Path.GetTempPath(), "3dgod-ci-anny-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            await using var svc = new AnnyHumanService(new WorkerProcessHost());
            var glb = await svc.GenerateGlbAsync(dest);
            Assert.True(File.Exists(glb), "Anny did not write GLB.");
            Assert.True(new FileInfo(glb).Length > 64);
            var doc = CanonicalGltfPipeline.Load(glb);
            Assert.True(doc.VertexCount >= 100, $"Expected real Anny mesh, got {doc.VertexCount} verts.");
            Assert.True(doc.TriangleCount >= 100);
        }
        finally
        {
            if (File.Exists(dest)) File.Delete(dest);
            var obj = Path.ChangeExtension(dest, ".obj");
            if (File.Exists(obj)) File.Delete(obj);
        }
    }

    [SkippableFact(Timeout = 900000)]
    public async Task Anny_HeightTallerDelta_IsNonUniformMeshChange()
    {
        RequireIntegrationOrGate("Anny height morph");

        var probe = AnnyRuntime.Probe(RepoPaths.FindRepoRoot());
        Assert.False(
            probe.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled,
            "Anny runtime missing while CI integration requested. Blocker: " + probe.Message);

        await using var svc = new AnnyHumanService(new WorkerProcessHost());
        var catalog = await svc.GetCatalogAsync();
        Assert.True(catalog.PhenotypeKeys.Count > 0, "Anny catalog returned no phenotype keys.");
        var key = AnnyHeightMorph.ResolvePhenotypeKey(catalog.PhenotypeKeys, catalog.LocalChangeKeys);
        Assert.Contains(key, catalog.PhenotypeKeys.Concat(catalog.LocalChangeKeys), StringComparer.OrdinalIgnoreCase);

        var shortPath = Path.Combine(Path.GetTempPath(), "3dgod-ci-anny-short-" + Guid.NewGuid().ToString("N") + ".glb");
        var tallPath = Path.Combine(Path.GetTempPath(), "3dgod-ci-anny-tall-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            var intoLocal = catalog.LocalChangeKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
            var shortHuman = new ThreeDGod.Core.Domain.ParametricHumanState();
            var tallHuman = new ThreeDGod.Core.Domain.ParametricHumanState();
            if (intoLocal)
            {
                shortHuman.LocalShapeParameters[key] = 0.1f;
                tallHuman.LocalShapeParameters[key] = 0.9f;
            }
            else
            {
                shortHuman.PhenotypeParameters[key] = 0.1f;
                tallHuman.PhenotypeParameters[key] = 0.9f;
            }

            shortHuman.PhenotypeParameters[AnnyHeightMorph.ProductParameterKey] = 0.1f;
            tallHuman.PhenotypeParameters[AnnyHeightMorph.ProductParameterKey] = 0.9f;

            await svc.GenerateGlbAsync(shortPath, AnnyHumanService.FromState(shortHuman));
            await svc.GenerateGlbAsync(tallPath, AnnyHumanService.FromState(tallHuman));

            var shortVerts = MeshCompare.ReadPositions(shortPath);
            var tallVerts = MeshCompare.ReadPositions(tallPath);
            Assert.True(shortVerts.Count >= 100, $"Expected real Anny mesh, got {shortVerts.Count} verts.");
            Assert.Equal(shortVerts.Count, tallVerts.Count);

            static (float height, float width) Bounds(System.Collections.Generic.IReadOnlyList<System.Numerics.Vector3> verts)
            {
                var min = verts[0];
                var max = verts[0];
                foreach (var v in verts)
                {
                    min = System.Numerics.Vector3.Min(min, v);
                    max = System.Numerics.Vector3.Max(max, v);
                }
                var size = max - min;
                return (size.Y, MathF.Max(size.X, size.Z));
            }

            var (beforeH, beforeW) = Bounds(shortVerts);
            var (afterH, afterW) = Bounds(tallVerts);
            Assert.True(
                AnnyHeightMorph.LooksLikeNonUniformHeightChange(beforeH, afterH, beforeW, afterW),
                $"Expected non-uniform taller morph; height {beforeH:F3}->{afterH:F3}, width {beforeW:F3}->{afterW:F3}, key={key}.");
            Assert.False(
                MeshCompare.IsUniformScale(shortVerts, tallVerts),
                "Taller phenotype must not be a uniform XYZ scale of the short mesh.");
        }
        finally
        {
            foreach (var p in new[] { shortPath, tallPath })
            {
                if (File.Exists(p)) File.Delete(p);
                var obj = Path.ChangeExtension(p, ".obj");
                if (File.Exists(obj)) File.Delete(obj);
            }
        }
    }

    [SkippableFact(Timeout = 600000)]
    public async Task GarmentCode_CpuJacket_WritesRealGlb()
    {
        RequireIntegrationOrGate("GarmentCode");

        var probe = GarmentCodeRuntime.Probe(RepoPaths.FindRepoRoot());
        Assert.False(
            probe.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled,
            "GarmentCode runtime missing while CI integration requested. Blocker: " + probe.Message);

        var dest = Path.Combine(Path.GetTempPath(), "3dgod-ci-gc-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            await using var svc = new GarmentCodeService(new WorkerProcessHost());
            var glb = await svc.GenerateJacketGlbAsync(dest, new GarmentCodeJacketRequest
            {
                SleeveLengthCm = 45,
                LengthCm = 60,
                WidthCm = 40
            });
            Assert.True(File.Exists(glb));
            Assert.True(new FileInfo(glb).Length > 64);
            var doc = CanonicalGltfPipeline.Load(glb);
            Assert.True(doc.VertexCount >= 12, $"Expected jacket mesh, got {doc.VertexCount} verts.");
        }
        finally
        {
            foreach (var ext in new[] { ".glb", ".obj", ".json" })
            {
                var p = Path.ChangeExtension(dest, ext);
                if (File.Exists(p)) File.Delete(p);
            }
        }
    }

    [SkippableFact]
    public void Cuda_IsClassifiedNotMocked()
    {
        var hasNvidia = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "nvidia-smi.exe"))
            || FindOnPath("nvidia-smi.exe") is not null;
        if (!hasNvidia)
            TestGate.Gpu("GitHub-hosted / current machine has no nvidia-smi; CUDA inference not executed (not mocked).");

        // If GPU present, still do not claim model inference without checkpoints.
        TestGate.ModelDownload("CUDA device detected but gated AI checkpoints are not installed/verified in this suite.");
    }

    [SkippableFact]
    public void Ue5_RealImport_RemainsGated()
    {
        TestGate.Ue5(
            "Real Unreal Engine editor import cannot run on GitHub-hosted runners: UE5 is not installed, " +
            "Epic licensing/credentials are unavailable, and GLB/FBX structural checks are not equivalent.");
    }

    [SkippableFact]
    public void ManualVisual_RemainsGated()
    {
        TestGate.ManualVisual(
            "Subjective visual quality (AI look, creature deformation, clothing motion, UE5 look) requires human review.");
    }

    private static string? FindOnPath(string file)
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(dir, file);
                if (File.Exists(candidate))
                    return candidate;
            }
            catch { /* ignore */ }
        }
        return null;
    }
}
