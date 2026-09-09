using ThreeDGod.Infrastructure;
using ThreeDGodCreator.Core.Localization;

namespace ThreeDGodCreator.Core.Tests;

public class LocalizationTests
{
    [Fact]
    public void Loc_DeAndEn_ReturnExpectedValues()
    {
        Assert.Equal("Rückgängig", Loc.GetForLocale("de", "action.undo"));
        Assert.Equal("Undo", Loc.GetForLocale("en", "action.undo"));
        Assert.Equal("Einstellungen", Loc.GetForLocale("de", "tab.settings"));
        Assert.Equal("Settings", Loc.GetForLocale("en", "tab.settings"));
    }

    [Fact]
    public void LocalizationCatalog_BridgesToResx()
    {
        Assert.Equal("Rückgängig", LocalizationCatalog.Get("de", "action.undo"));
        Assert.Equal("Undo", LocalizationCatalog.Get("en", "action.undo"));
    }

    [Fact]
    public void MissingKey_DoesNotShowRawKey()
    {
        const string missing = "phase53.missing.key.xyz";
        var de = Loc.GetForLocale("de", missing);
        var en = Loc.GetForLocale("en", missing);

        Assert.DoesNotContain(missing, de, StringComparison.Ordinal);
        Assert.DoesNotContain(missing, en, StringComparison.Ordinal);
        Assert.Equal("…", de);
        Assert.Equal("…", en);
    }

    [Fact]
    public void SetCulture_SwitchesActiveStrings()
    {
        try
        {
            Loc.SetCulture("de");
            Assert.Equal("de", Loc.CurrentLocale);
            Assert.Equal("Rückgängig", Loc.Get("action.undo"));

            Loc.SetCulture("en");
            Assert.Equal("en", Loc.CurrentLocale);
            Assert.Equal("Undo", Loc.Get("action.undo"));
        }
        finally
        {
            Loc.SetCulture("en");
        }
    }

    [Fact]
    public void SetCulture_RaisesChangedEvent()
    {
        var calls = 0;
        void Handler() => calls++;
        Loc.CultureChanged += Handler;
        try
        {
            Loc.SetCulture("de");
            Loc.SetCulture("de");
            Loc.SetCulture("en");
            Assert.Equal(2, calls);
        }
        finally
        {
            Loc.CultureChanged -= Handler;
            Loc.SetCulture("en");
        }
    }
}
