using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure.Logging;
using ThreeDGodCreator.App.Localization;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Localization;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App.Panels;

public partial class SettingsPanel : UserControl, ILocalizableView
{
    private readonly CharacterSystem _characterSystem;
    private readonly ConfigService _configService;
    private readonly IBlenderOperations _blenderService;
    private readonly Window _mainWindow;
    private bool _suppressLanguageChange;

    public SettingsPanel(CharacterSystem cs, ConfigService configService, IBlenderOperations blenderService, Window mainWindow, IFeatureAvailabilityService features)
    {
        InitializeComponent();
        _characterSystem = cs;
        _configService = configService;
        _blenderService = blenderService;
        _mainWindow = mainWindow;

        var cfg = _configService.Load();
        TxtBlenderPath.Text = cfg.BlenderPath;
        SelectTheme(cfg.Theme);
        SelectLanguage(cfg.Language);
        ChkNsfw.IsChecked = cfg.NsfwEnabled;
        ChkController.IsChecked = cfg.ControllerEnabled;

        TxtBlenderPath.LostFocus += (_, _) => SaveConfig();
        if (!features.IsInvocable(FeatureIds.ControllerInput))
        {
            ChkController.IsEnabled = false;
            ChkController.ToolTip = features.GetStatusMessage(FeatureIds.ControllerInput);
        }

        ApplyLocalization();
    }

    public void ApplyLocalization()
    {
        LblTitle.Text = Loc.Get("settings.title");
        LblLanguage.Text = Loc.Get("settings.language");
        LblLegacyRuntime.Text = Loc.Get("settings.legacy_runtime");
        LblTheme.Text = Loc.Get("settings.theme");
        ChkNsfw.Content = Loc.Get("settings.nsfw");
        ChkController.Content = Loc.Get("settings.controller");
        BtnTestBlender.Content = Loc.Get("settings.test_runtime");
        BtnTestBlender.ToolTip = Loc.Get("settings.test_runtime.tooltip");
        BtnDiagnostics.Content = Loc.Get("settings.system_check");
        BtnDiagnostics.ToolTip = Loc.Get("settings.system_check.tooltip");
        BtnOpenLogs.Content = Loc.Get("settings.open_logs");

        var selectedLanguage = (CmbLanguage.SelectedItem as ComboBoxItem)?.Tag as string ?? Loc.CurrentLocale;
        var selectedTheme = (CmbTheme.SelectedItem as ComboBoxItem)?.Tag as string ?? "dark";

        _suppressLanguageChange = true;
        CmbLanguage.Items.Clear();
        CmbLanguage.Items.Add(new ComboBoxItem { Tag = "de", Content = Loc.Get("settings.language.de") });
        CmbLanguage.Items.Add(new ComboBoxItem { Tag = "en", Content = Loc.Get("settings.language.en") });
        SelectLanguage(selectedLanguage);

        CmbTheme.Items.Clear();
        CmbTheme.Items.Add(new ComboBoxItem { Tag = "dark", Content = Loc.Get("settings.theme.dark") });
        CmbTheme.Items.Add(new ComboBoxItem { Tag = "light", Content = Loc.Get("settings.theme.light") });
        CmbTheme.Items.Add(new ComboBoxItem { Tag = "cyberpunk", Content = Loc.Get("settings.theme.cyberpunk") });
        SelectTheme(selectedTheme);
        _suppressLanguageChange = false;
    }

    private void SelectLanguage(string locale)
    {
        for (var i = 0; i < CmbLanguage.Items.Count; i++)
        {
            if (CmbLanguage.Items[i] is ComboBoxItem item &&
                string.Equals(item.Tag as string, locale, StringComparison.OrdinalIgnoreCase))
            {
                CmbLanguage.SelectedIndex = i;
                return;
            }
        }
        CmbLanguage.SelectedIndex = locale.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
    }

    private void SelectTheme(string theme)
    {
        for (var i = 0; i < CmbTheme.Items.Count; i++)
        {
            if (CmbTheme.Items[i] is ComboBoxItem item &&
                string.Equals(item.Tag as string, theme, StringComparison.OrdinalIgnoreCase))
            {
                CmbTheme.SelectedIndex = i;
                return;
            }
        }
        CmbTheme.SelectedIndex = 0;
    }

    private void SaveConfig()
    {
        var cfg = _configService.Load();
        cfg.BlenderPath = TxtBlenderPath.Text;
        cfg.Theme = (CmbTheme.SelectedItem as ComboBoxItem)?.Tag as string ?? "dark";
        cfg.Language = (CmbLanguage.SelectedItem as ComboBoxItem)?.Tag as string ?? "de";
        cfg.NsfwEnabled = ChkNsfw.IsChecked == true;
        cfg.ControllerEnabled = ChkController.IsChecked == true;
        _characterSystem.NsfwEnabled = cfg.NsfwEnabled;
        _configService.Save(cfg);
    }

    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e) => SaveConfig();

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLanguageChange || CmbLanguage.SelectedItem is not ComboBoxItem item)
            return;

        var locale = item.Tag as string ?? "de";
        SaveConfig();
        Loc.SetCulture(locale);
    }

    private void ChkNsfw_Changed(object sender, RoutedEventArgs e) => SaveConfig();
    private void ChkController_Changed(object sender, RoutedEventArgs e) => SaveConfig();

    private void BtnBrowseBlender_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Runtime (blender.exe)|blender.exe|Alle Dateien|*.*",
            Title = "Optionales Legacy-Runtime auswählen"
        };
        if (dlg.ShowDialog() == true)
        {
            TxtBlenderPath.Text = dlg.FileName;
            SaveConfig();
        }
    }

    private void BtnTestBlender_Click(object sender, RoutedEventArgs e)
    {
        SaveConfig();
        if (_blenderService.VerifyCanLaunch(out var error))
        {
            var path = _blenderService.GetBlenderPath();
            MessageBox.Show($"Legacy-Runtime ist verfügbar (headless).\n\nPfad: {path}", "Runtime OK", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Legacy-Runtime ist nicht verfügbar.\n\n{error}\n\nDas ist kein App-Absturz. Optionalen Pfad in den Einstellungen setzen, falls du den Fallback brauchst.", "Runtime unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        var report = DiagnosticsService.RunSystemCheck();
        MessageBox.Show(report, "System-Check", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnOpenLogs_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            GodLog.OpenLogFolder();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Log-Ordner konnte nicht geöffnet werden: {ex.Message}", "Logs", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
