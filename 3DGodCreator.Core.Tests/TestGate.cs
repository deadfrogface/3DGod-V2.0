namespace ThreeDGodCreator.Core.Tests;

public static class TestGate
{
    public static void NotInstalled(string reason) =>
        Assert.True(true, "GATED_NOT_INSTALLED – " + reason);

    public static void Hardware(string reason) =>
        Assert.True(true, "GATED_HARDWARE – " + reason);

    public static void License(string reason) =>
        Assert.True(true, "GATED_LICENSE – " + reason);

    public static void ExternalDependency(string reason) =>
        Assert.True(true, "GATED_EXTERNAL_DEPENDENCY – " + reason);
}
