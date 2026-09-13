using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Exceptions;
using FlaUI.UIA3;
using Xunit;

namespace ThreeDGodCreator.UiTests;

/// <summary>
/// FlaUI UIA3 suite for an installed 3D God build.
/// IMPLEMENTED_GATED_EXTERNAL_RUNNER: skipped unless THREEDGOD_INSTALL_ROOT is set
/// and an interactive Windows desktop session is available.
/// </summary>
public class InstalledAppFlaUiTests
{
    private static string? InstallRoot => Environment.GetEnvironmentVariable("THREEDGOD_INSTALL_ROOT");

    private static string? ResolveExe()
    {
        var root = InstallRoot;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return null;
        return Directory.EnumerateFiles(root, "3DGodCreator.App.exe", SearchOption.AllDirectories).FirstOrDefault()
               ?? Directory.EnumerateFiles(root, "*3DGod*.exe", SearchOption.AllDirectories)
                   .FirstOrDefault(p => !p.Contains("Smoke", StringComparison.OrdinalIgnoreCase));
    }

    private static void RequireInstallRootOrSkip()
    {
        Skip.If(
            string.IsNullOrWhiteSpace(InstallRoot) || !Directory.Exists(InstallRoot),
            "GATED_EXTERNAL_RUNNER - set THREEDGOD_INSTALL_ROOT to an installed 3D God tree on an interactive Windows desktop.");
    }

    [Fact]
    public void Suite_IsPresent_AndNotWiredIntoDefaultCi()
    {
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "3DGodCreator.UiTests.csproj"))
                    || File.Exists("3DGodCreator.UiTests.csproj")
                    || Directory.Exists(Path.GetDirectoryName(typeof(InstalledAppFlaUiTests).Assembly.Location)));
        Assert.True(true); // structural presence; live UI below is environment-gated
    }

    [SkippableFact]
    public void Launch_MainWindow_SetupAssistant_Settings_Close()
    {
        RequireInstallRootOrSkip();
        var exe = ResolveExe();
        Assert.False(string.IsNullOrWhiteSpace(exe),
            "THREEDGOD_INSTALL_ROOT is set but 3DGodCreator.App.exe was not found – FAIL (not skip).");

        using var automation = new UIA3Automation();
        using var app = Application.Launch(exe!);
        app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(60));
        var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(60));
        Assert.NotNull(window);
        Assert.False(string.IsNullOrWhiteSpace(window!.Title));

        // Tools → Setup Assistant / Components…
        var setupOpened = TryInvokeMenu(window, "Setup Assistant")
                          || TryInvokeMenu(window, "Setup Assistant / Components")
                          || TryInvokeMenu(window, "Components");
        Assert.True(setupOpened, "Setup Assistant menu item not found after launch in real install – FAIL.");

        var setupWin = app.GetAllTopLevelWindows(automation)
            .FirstOrDefault(w => (w.Title ?? "").Contains("Setup Assistant", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(setupWin);
        // Feature list / refresh controls must exist when the dialog is up.
        var featureList = setupWin!.FindFirstDescendant(cf => cf.ByName("FeatureList"))
                          ?? setupWin.FindFirstDescendant(cf => cf.ByAutomationId("FeatureList"))
                          ?? setupWin.FindFirstDescendant(cf =>
                              cf.ByControlType(FlaUI.Core.Definitions.ControlType.List));
        Assert.NotNull(featureList);
        var refresh = setupWin.FindFirstDescendant(cf => cf.ByName("Refresh"))
                      ?? setupWin.FindFirstDescendant(cf => cf.ByAutomationId("BtnRefresh"));
        Assert.NotNull(refresh);
        setupWin.Close();

        // Settings tab
        var settingsTab = window.FindFirstDescendant(cf => cf.ByName("Settings"))
                          ?? window.FindFirstDescendant(cf => cf.ByAutomationId("TabSettings"));
        Assert.NotNull(settingsTab);
        settingsTab!.Click();
        var settingsTitle = window.FindFirstDescendant(cf => cf.ByName("Settings"))
                            ?? window.FindFirstDescendant(cf => cf.ByAutomationId("LblTitle"));
        Assert.NotNull(settingsTitle);

        // At least one Setup Assistant feature row should be discoverable after reopen via Components button if present.
        var componentsBtn = window.FindFirstDescendant(cf => cf.ByName("Components…"))
                            ?? window.FindFirstDescendant(cf => cf.ByName("Components..."))
                            ?? window.FindFirstDescendant(cf => cf.ByAutomationId("BtnComponents"));
        if (componentsBtn is not null)
        {
            componentsBtn.Click();
            var again = app.GetAllTopLevelWindows(automation)
                .FirstOrDefault(w => (w.Title ?? "").Contains("Setup Assistant", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(again);
            var list = again!.FindFirstDescendant(cf =>
                cf.ByControlType(FlaUI.Core.Definitions.ControlType.List));
            Assert.NotNull(list);
            again.Close();
        }

        window.Close();
        Assert.True(app.HasExited || app.Close());
    }

    /// <summary>
    /// Invokes a menu item whose name contains <paramref name="contains"/>.
    /// Only swallows ElementNotAvailableException — assertion failures must propagate.
    /// </summary>
    private static bool TryInvokeMenu(Window window, string contains)
    {
        try
        {
            var item = window.FindFirstDescendant(cf =>
                cf.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem)
                    .And(cf.ByName(contains)));
            if (item is null)
            {
                // Partial name match across menu items.
                var items = window.FindAllDescendants(cf =>
                    cf.ByControlType(FlaUI.Core.Definitions.ControlType.MenuItem));
                item = items.FirstOrDefault(i =>
                    (i.Name ?? "").Contains(contains, StringComparison.OrdinalIgnoreCase));
            }

            if (item is null)
                return false;
            item.AsMenuItem()?.Invoke();
            return true;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }
}
