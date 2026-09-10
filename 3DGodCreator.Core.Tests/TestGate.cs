namespace ThreeDGodCreator.Core.Tests;

/// <summary>
/// Explicit environment gates. Requires <c>[SkippableFact]</c>/<c>[SkippableTheory]</c> on the calling test.
/// Outcomes are xUnit <b>Skipped</b> (not Passed), so CI can count GATED_* separately from real PASS.
/// </summary>
public static class TestGate
{
    public static void NotInstalled(string reason) =>
        Skip.If(true, "GATED_NOT_INSTALLED - " + reason);

    public static void Hardware(string reason) =>
        Skip.If(true, "GATED_HARDWARE - " + reason);

    public static void License(string reason) =>
        Skip.If(true, "GATED_LICENSE - " + reason);

    public static void ExternalDependency(string reason) =>
        Skip.If(true, "GATED_EXTERNAL_DEPENDENCY - " + reason);

    public static void ExternalRuntime(string reason) =>
        Skip.If(true, "GATED_EXTERNAL_RUNTIME - " + reason);

    public static void Gpu(string reason) =>
        Skip.If(true, "GATED_GPU - " + reason);

    public static void ModelLicense(string reason) =>
        Skip.If(true, "GATED_MODEL_LICENSE - " + reason);

    public static void ModelDownload(string reason) =>
        Skip.If(true, "GATED_MODEL_DOWNLOAD - " + reason);

    public static void Ue5(string reason) =>
        Skip.If(true, "GATED_UE5 - " + reason);

    public static void ManualVisual(string reason) =>
        Skip.If(true, "GATED_MANUAL_VISUAL - " + reason);

    public static void Other(string reason) =>
        Skip.If(true, "GATED_OTHER - " + reason);
}
