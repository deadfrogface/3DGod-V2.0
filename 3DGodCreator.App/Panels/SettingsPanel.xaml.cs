using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGod.Application;
using ThreeDGod.Infrastructure.Logging;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App.Panels;

public partial class SettingsPanel : UserControl
{
    private readonly CharacterSystem _characterSystem;
    private readonly ConfigService _configService;
    private readonly IBlenderOperations _blenderService;
    private readonly Window _mainWindow;

    public SettingsPanel(CharacterSystem cs, ConfigService configService, IBlenderOperations blenderService, Window mainWindow, IFeatureAvailabilityService features)
    {
        InitializeComponent();
        _characterSystem = cs;
        _configService = configService;
        _blenderService = blenderService;
        _mainWindow = mainWindow;

        var cfg = _configService.Load();
        TxtBlenderPath.Text = cfg.BlenderPath;
        CmbTheme.SelectedIndex = cfg.Theme switch { "light" => 1, "cyberpunk" => 2, _ => 0 };
        ChkNsfw.IsChecked = cfg.NsfwEnabled;
        ChkController.IsChecked = cfg.ControllerEnabled;

        TxtBlenderPath.LostFocus += (_, _) => SaveConfig();
        if (!features.IsInvocable(FeatureIds.ControllerInput))
        {
            ChkController.IsEnabled = false;
            ChkController.ToolTip = features.GetStatusMessage(FeatureIds.ControllerInput);
        }
    }

    private void SaveConfig()
    {
        var cfg = _configService.Load();
        cfg.BlenderPath = TxtBlenderPath.Text;
        cfg.Theme = CmbTheme.SelectedIndex switch { 1 => "light", 2 => "cyberpunk", _ => "dark" };
        cfg.NsfwEnabled = ChkNsfw.IsChecked == true;
        cfg.ControllerEnabled = ChkController.IsChecked == true;
        _characterSystem.NsfwEnabled = cfg.NsfwEnabled;
        _configService.Save(cfg);
    }

    private void CmbTheme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => SaveConfig();
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
