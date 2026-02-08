using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App.Panels;

public partial class SettingsPanel : UserControl
{
    private readonly ConfigService _configService;
    private readonly Window _mainWindow;

    public SettingsPanel(CharacterSystem cs, ConfigService configService, Window mainWindow)
    {
        InitializeComponent();
        _configService = configService;
        _mainWindow = mainWindow;

        var cfg = _configService.Load();
        TxtBlenderPath.Text = cfg.BlenderPath;
        CmbTheme.SelectedIndex = cfg.Theme == "light" ? 1 : 0;

        TxtBlenderPath.LostFocus += (_, _) => SaveConfig();
        CmbTheme.SelectionChanged += (_, _) => SaveConfig();
    }

    private void SaveConfig()
    {
        var cfg = _configService.Load();
        cfg.BlenderPath = TxtBlenderPath.Text;
        cfg.Theme = CmbTheme.SelectedIndex == 1 ? "light" : "dark";
        _configService.Save(cfg);
    }

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
