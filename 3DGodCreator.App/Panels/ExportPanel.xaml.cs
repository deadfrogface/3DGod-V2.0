using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGod.Application;
using ThreeDGod.Export;
using ThreeDGodCreator.App;
using ThreeDGodCreator.App.Localization;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Localization;

namespace ThreeDGodCreator.App.Panels;

public partial class ExportPanel : UserControl, ILocalizableView
{
    private readonly CharacterSystem _cs;
    private readonly string _basePath;
    private readonly Func<string> _currentPreview;
    private readonly IFbxExportService _fbxExport;
    private readonly ActiveProjectSession? _session;

    private readonly IFeatureAvailabilityService _features;

    public ExportPanel(
        CharacterSystem cs,
        IFeatureAvailabilityService features,
        IFbxExportService fbxExport,
        Func<string> currentPreview,
        ActiveProjectSession? session = null)
    {
        InitializeComponent();
        _cs = cs;
        _features = features;
        _fbxExport = fbxExport;
        _currentPreview = currentPreview;
        _session = session;
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
        BtnSavePreset.IsEnabled = _features.IsInvocable(FeatureIds.PresetSave);
        BtnExportFbx.IsEnabled = _features.IsInvocable(FeatureIds.ExportFbx);
        // This prepares an UE5-ready FBX file; it does not claim editor import.
        BtnExportUnreal.IsEnabled = _features.IsInvocable(FeatureIds.ExportFbx);
        BtnExportGlb.IsEnabled = _features.IsInvocable(FeatureIds.ExportGlb);
        WriteLog(_features.GetStatusMessage(FeatureIds.PresetSave), "INFO");
        WriteLog(_features.GetStatusMessage(FeatureIds.ExportFbx), "INFO");
        WriteLog(_features.GetStatusMessage(FeatureIds.ExportGlb), "INFO");
        WriteLog("UE5: Preflight + optional external smoke only. No in-app editor import.", "INFO");
        WriteLog(_features.GetStatusMessage(FeatureIds.ExportUnreal), "INFO");
        ApplyLocalization();
    }

    public void ApplyLocalization()
    {
        LblTitle.Text = Loc.Get("export.title");
        LblFilename.Text = Loc.Get("export.filename");
        BtnSavePreset.Content = Loc.Get("export.save_preset");
        BtnExportFbx.Content = Loc.Get("export.fbx");
        BtnExportGlb.Content = Loc.Get("export.glb");
        LblUnrealFolder.Text = Loc.Get("export.unreal_folder");
        BtnBrowseUnreal.Content = Loc.Get("export.choose_folder");
        BtnExportUnreal.Content = Loc.Get("export.to_unreal");
        LblLog.Text = Loc.Get("export.log");
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
        WriteLog("FBX-Export ist Experimental – kein UE5-Editor-Import wird behauptet.", "INFO");
        WriteLog(_features.GetStatusMessage(FeatureIds.ExportFbx), "INFO");

        var src = _currentPreview();
        if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
            src = _cs.ResolveExportGlbSource() ?? "";

        if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
        {
            WriteLog("Kein Viewport-/Character-GLB für FBX-Export gefunden.", "ERROR");
            return;
        }

        var fbx = Path.Combine(_basePath, "exports", $"{name}.fbx");
        try
        {
            _cs.SavePreset(name);
            WriteLog($"GLB → FBX (headless Blender): {src} → {fbx}", "INFO");
            _fbxExport.Export(src, fbx, name, runUe5Preflight: true);

            var sanity = FbxSanity.Check(fbx);
            foreach (var issue in sanity.Issues)
                WriteLog(issue, sanity.Passed ? "INFO" : "WARN");

            if (sanity.Passed)
                WriteLog($"FBX geschrieben und sanity-geprüft: {fbx}", "INFO");
            else
                WriteLog($"FBX sanity-check fehlgeschlagen: {fbx}", "ERROR");
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
            var work = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "ExportWork");
            src = _session?.GetActiveMeshGlbPathOrMaterialize(work) ?? "";
        }
        if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
        {
            WriteLog("Kein Projekt-/Viewport-GLB zum Export.", "WARN");
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
            var assetName = Path.GetFileNameWithoutExtension(dlg.FileName);
            var preflight = UnrealEngine5ExportProfile.EvaluateGlb(src, assetName);
            foreach (var soft in preflight.SoftMessages)
                WriteLog(soft, "INFO");
            if (!preflight.Passed)
            {
                foreach (var hard in preflight.HardMessages)
                    WriteLog(hard, "ERROR");
                WriteLog("UE5-Preflight Hard-Fail – Export abgebrochen.", "ERROR");
                return;
            }

            GlbExportService.Export(src, dlg.FileName);
            WriteLog($"GLB geschrieben: {dlg.FileName}", "INFO");
            WriteLog("Hinweis: Preflight ≠ erfolgreicher UE5-Editor-Import.", "INFO");
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
        try
        {
            var targetDir = TxtUnrealPath.Text.Trim();
            if (targetDir.Length == 0 || !Directory.Exists(targetDir))
                throw new InvalidOperationException("Choose an existing UE5 project/import folder first.");

            var src = _currentPreview();
            if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
            {
                var work = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "3DGod", "ExportWork");
                src = _session?.GetActiveMeshGlbPathOrMaterialize(work) ?? "";
            }
            if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
                throw new InvalidOperationException("No active character GLB to export.");

            var name = TxtFilename.Text.Trim();
            if (name.Length == 0) name = "character";
            var preflight = UnrealEngine5ExportProfile.EvaluateGlb(src, name);
            foreach (var soft in preflight.SoftMessages) WriteLog(soft, "INFO");
            if (!preflight.Passed)
                throw new InvalidOperationException("UE5 preflight blocked export: " + string.Join("; ", preflight.HardMessages));

            var destination = Path.Combine(targetDir, name + ".fbx");
            _fbxExport.Export(src, destination, name, runUe5Preflight: true);
            var sanity = FbxSanity.Check(destination);
            if (!sanity.Passed)
                throw new InvalidOperationException("FBX sanity check failed: " + string.Join("; ", sanity.Issues));

            WriteLog($"UE5-ready FBX prepared: {destination}", "SUCCESS");
            WriteLog("Editor import remains external/gated; no successful UE5 import is claimed.", "INFO");
        }
        catch (Exception ex)
        {
            WriteLog("UE5 preparation failed: " + ex.Message, "ERROR");
            MessageBox.Show(ex.Message, "UE5 preparation", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
