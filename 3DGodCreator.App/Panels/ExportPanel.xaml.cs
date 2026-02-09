using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGodCreator.App;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class ExportPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private readonly string _basePath;

    public ExportPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
    }

    private void WriteLog(string message, string level = "INFO")
    {
        var line = $"[{level}] {message}";
        TxtLog.AppendText(line + "\n");
        TxtLog.ScrollToEnd();
        DebugLog.Write(line);
    }

    private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtFilename.Text.Trim();
        if (string.IsNullOrEmpty(name)) name = "my_character";
        WriteLog($"Speichere Preset: {name}");
        _cs.SavePreset(name);
        WriteLog($"Preset gespeichert: {name}", "SUCCESS");
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtFilename.Text.Trim();
        if (string.IsNullOrEmpty(name)) name = "my_character";
        WriteLog("Starte FBX-Export");
        try
        {
            _cs.SavePreset(name);
            _cs.ExportFbx(name);
            WriteLog($"FBX-Export abgeschlossen: exports/{name}.fbx", "SUCCESS");
        }
        catch (Exception ex)
        {
            WriteLog($"Fehler beim Export: {ex.Message}", "ERROR");
        }
    }

    private void BtnBrowseUnreal_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Unreal-Zielordner: Wähle eine beliebige Datei im Zielordner",
            CheckFileExists = true
        };
        if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(dlg.FileName))
        {
            var dir = Path.GetDirectoryName(dlg.FileName);
            if (!string.IsNullOrEmpty(dir))
            {
                TxtUnrealPath.Text = dir;
                WriteLog($"Unreal-Zielordner: {dir}", "SUCCESS");
            }
        }
    }

    private void BtnExportToUnreal_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtFilename.Text.Trim();
        if (string.IsNullOrEmpty(name)) name = "my_character";
        var dstDir = TxtUnrealPath.Text.Trim();
        var srcFbx = Path.Combine(_basePath, "exports", $"{name}.fbx");

        if (string.IsNullOrEmpty(dstDir) || !Directory.Exists(dstDir))
        {
            WriteLog("Ungültiger Unreal-Zielpfad.", "ERROR");
            return;
        }
        if (!File.Exists(srcFbx))
        {
            WriteLog($"FBX nicht gefunden. Zuerst exportieren: {srcFbx}", "ERROR");
            return;
        }

        try
        {
            var dstFbx = Path.Combine(dstDir, $"{name}.fbx");
            File.Copy(srcFbx, dstFbx, overwrite: true);
            WriteLog($"FBX kopiert nach Unreal: {dstFbx}", "SUCCESS");
        }
        catch (Exception ex)
        {
            WriteLog($"Fehler beim Kopieren: {ex.Message}", "ERROR");
        }
    }
}
