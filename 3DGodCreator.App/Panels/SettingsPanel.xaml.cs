using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App.Panels;

public partial class SettingsPanel : UserControl
{
    private readonly CharacterSystem _characterSystem;
    private readonly ConfigService _configService;
    private readonly Window _mainWindow;

    public SettingsPanel(CharacterSystem cs, ConfigService configService, Window mainWindow)
    {
        InitializeComponent();
        _characterSystem = cs;
        _configService = configService;
        _mainWindow = mainWindow;

        var cfg = _configService.Load();
        TxtBlenderPath.Text = cfg.BlenderPath;
        CmbTheme.SelectedIndex = cfg.Theme switch { "light" => 1, "cyberpunk" => 2, _ => 0 };
        ChkNsfw.IsChecked = cfg.NsfwEnabled;
        ChkController.IsChecked = cfg.ControllerEnabled;

        TxtBlenderPath.LostFocus += (_, _) => SaveConfig();
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
            Filter = "Blender (blender.exe)|blender.exe|Alle Dateien|*.*",
            Title = "Blender auswählen"
        };
        if (dlg.ShowDialog() == true)
        {
            TxtBlenderPath.Text = dlg.FileName;
            SaveConfig();
        }
    }

    private void BtnDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        var report = DiagnosticsService.RunSystemCheck();
        MessageBox.Show(report, "System-Check", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
