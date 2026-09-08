using System.Text.Json;

namespace ThreeDGodCreator.Core.Tests;

public class BodyParametersTests
{
    [Fact]
    public void BodyParametersJson_DeserializesAndContainsHeight()
    {
        var path = Path.Combine(RepoPaths.AssetsDir, "body_parameters.json");
        Assert.True(File.Exists(path), "assets/body_parameters.json must exist in the repository.");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        Assert.True(doc.RootElement.TryGetProperty("height", out var height));
        Assert.Equal("Größe", height.GetProperty("label").GetString());
        Assert.Equal(0, height.GetProperty("min").GetInt32());
        Assert.Equal(100, height.GetProperty("max").GetInt32());
        Assert.Equal(50, height.GetProperty("default").GetInt32());
        Assert.True(doc.RootElement.EnumerateObject().Count() >= 10);
    }
}
