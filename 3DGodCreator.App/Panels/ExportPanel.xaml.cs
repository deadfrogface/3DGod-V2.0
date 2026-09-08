using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGod.Application;
using ThreeDGod.Export;
using ThreeDGodCreator.App;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class ExportPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private readonly string _basePath;
    private readonly Func<string> _currentPreview;

    private readonly IFeatureAvailabilityService _features;

    public ExportPanel(CharacterSystem cs, IFeatureAvailabilityService features, Func<string> currentPreview)
    {
        InitializeComponent();
        _cs = cs;
        _features = features;
        _currentPreview = currentPreview;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
        BtnSavePreset.IsEnabled = _features.IsInvocable(FeatureIds.PresetSave);
        BtnExportFbx.IsEnabled = _features.IsInvocable(FeatureIds.ExportFbx);
        BtnExportUnreal.IsEnabled = _features.IsInvocable(FeatureIds.ExportUnreal);
        BtnExportGlb.IsEnabled = _features.IsInvocable(FeatureIds.ExportGlb);
        WriteLog(_features.GetStatusMessage(FeatureIds.PresetSave), "INFO");
        WriteLog(_features.GetStatusMessage(FeatureIds.ExportFbx), "INFO");
        WriteLog(_features.GetStatusMessage(FeatureIds.ExportGlb), "INFO");
        WriteLog(_features.GetStatusMessage(FeatureIds.ExportUnreal), "INFO");
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
        WriteLog("FBX-Export ist Experimental und wird nicht als Erfolg gemeldet, bevor eine Datei existiert.", "INFO");
        WriteLog(_features.GetStatusMessage(FeatureIds.ExportFbx), "INFO");
        try
        {
            _cs.SavePreset(name);
            _cs.ExportFbx(name);
            var fbx = Path.Combine(_basePath, "exports", $"{name}.fbx");
            if (File.Exists(fbx))
                WriteLog($"FBX-Datei vorhanden: {fbx}", "INFO");
            else
                WriteLog($"Kein FBX verifiziert unter {fbx}. Job wurde nur angestoßen.", "WARN");
        }
        catch (Exception ex)
        {
            WriteLog($"Fehler beim Export: {ex.Message}", "ERROR");
        }
    }

    private void BtnExportGlb_Click(object sender, RoutedEventArgs e)
    {
        var src = _currentPreview();
        if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
        {
            WriteLog("Kein verifiziertes Viewport-GLB zum Export.", "WARN");
            return;
        }
        var dlg = new SaveFileDialog
        {
            Filter = "GLB|*.glb",
            FileName = (TxtFilename.Text.Trim().Length == 0 ? "character" : TxtFilename.Text.Trim()) + ".glb"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            GlbExportService.Export(src, dlg.FileName);
            WriteLog($"GLB geschrieben: {dlg.FileName}", "INFO");
        }
        catch (Exception ex)
        {
            WriteLog($"GLB-Export fehlgeschlagen: {ex.Message}", "ERROR");
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
                WriteLog($"Unreal-Zielordner ausgewählt (Copy-Ziel, keine UE5-Pipeline): {dir}", "INFO");
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
            WriteLog($"FBX nach Ordner kopiert (kein UE5-Pipeline): {dstFbx}", "INFO");
            WriteLog(_features.GetStatusMessage(FeatureIds.ExportUnreal), "WARN");
        }
        catch (Exception ex)
        {
            WriteLog($"Fehler beim Kopieren: {ex.Message}", "ERROR");
        }
    }
}
