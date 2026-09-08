using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class DotNet10MigrationTests
{
    [Fact]
    public void Runtime_IsNet10OrNewer()
    {
        Assert.True(Environment.Version.Major >= 10,
            "Expected .NET 10 runtime, got " + Environment.Version);
    }

    [Fact]
    public void CheckDotNet_ReportsSuccess()
    {
        var result = ProjectReadinessService.CheckDotNet();
        Assert.True(result.Success, result.Message);
        Assert.Contains("10.", result.Message, StringComparison.Ordinal);
    }
}
