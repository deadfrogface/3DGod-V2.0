using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.Core.Tests;

public class FroggyServiceTests
{
    [Fact]
    public void AnalyzeLog_DetectsMissingBlenderAndOffersSettingsFix()
    {
        var result = FroggyService.AnalyzeLog("Blender wurde nicht gefunden. Pfad nicht konfiguriert.");
        Assert.Equal("Blender wurde nicht gefunden.", result.Problem);
        Assert.True(result.CanFix);
        Assert.Equal("OpenSettings", result.FixAction);
    }

    [Fact]
    public void AnalyzeLog_UnknownText_DoesNotInventASpecificFailure()
    {
        var result = FroggyService.AnalyzeLog("everything is fine");
        Assert.Equal("Kein spezifisches Problem erkannt.", result.Problem);
        Assert.False(result.CanFix);
    }
}
