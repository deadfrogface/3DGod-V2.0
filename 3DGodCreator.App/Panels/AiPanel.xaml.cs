using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Infrastructure;
using ThreeDGod.Workers;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class AiPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private readonly IFeatureAvailabilityService _features;
    private readonly AnnyHumanService _anny;
    private readonly IAssetGenerationService _assets;
    private readonly IImageTo3DService _imageTo3D;
    private readonly IReferenceImageGenerationService _referenceImages;
    private readonly ActiveProjectSession _session;
    private readonly AllowlistedAiEditExecutor _aiEdits;
    private readonly CommandStack _commands;
    private readonly Action<string> _loadPreview;
    private string? _selectedImagePath;

    public AiPanel(
        CharacterSystem cs,
        IFeatureAvailabilityService features,
        AnnyHumanService anny,
        Action<string> loadPreview,
        IAssetGenerationService assets,
        IImageTo3DService imageTo3D,
        IReferenceImageGenerationService referenceImages,
        ActiveProjectSession session,
        AllowlistedAiEditExecutor aiEdits,
        CommandStack commands)
    {
        InitializeComponent();
        _cs = cs;
        _features = features;
        _anny = anny;
        _assets = assets;
        _imageTo3D = imageTo3D;
        _referenceImages = referenceImages;
        _session = session;
        _aiEdits = aiEdits;
        _commands = commands;
        _loadPreview = loadPreview;
        var annyOk = _features.IsInvocable(FeatureIds.AnnyHuman);
        var personOk = _features.IsInvocable(FeatureIds.AiGeneratePerson);
        var assetOk = _features.IsInvocable(FeatureIds.AiGenerateAsset);
        var imageOk = _features.IsInvocable(FeatureIds.ImageTo3D);
        BtnAnnyHuman.IsEnabled = annyOk;
        BtnReferenceImage.IsEnabled = _features.IsInvocable(FeatureIds.ReferenceImageGenerate);
        BtnGeneratePerson.IsEnabled = personOk || _features.IsInvocable(FeatureIds.AiCommandInterpret);
        BtnGeneratePerson.Content = personOk
            ? "Erzeuge vollständige Person"
            : "AI-Edit (allowlisted)";
        BtnGenerateAsset.IsEnabled = assetOk || imageOk;
        TxtPrompt.IsEnabled = true;
        AvailabilityLabel.Text =
            $"Anny: {_features.GetStatusMessage(FeatureIds.AnnyHuman)}\n" +
            $"Referenzbild: {_features.GetStatusMessage(FeatureIds.ReferenceImageGenerate)}\n" +
            $"Image→3D: {_features.GetStatusMessage(FeatureIds.ImageTo3D)}\n" +
            $"AI-Edit: {_features.GetStatusMessage(FeatureIds.AiCommandInterpret)}\n" +
            $"Asset: {_features.GetStatusMessage(FeatureIds.AiGenerateAsset)}";
        StatusLabel.Text = annyOk
            ? _features.GetStatusMessage(FeatureIds.AnnyHuman)
            : _features.GetStatusMessage(FeatureIds.ImageTo3D);
    }

    private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Bilder|*.png;*.jpg;*.jpeg|Alle Dateien|*.*",
            Title = "Bild auswählen"
        };
        if (dlg.ShowDialog() == true)
        {
            _selectedImagePath = dlg.FileName;
            LblImage.Text = Path.GetFileName(_selectedImagePath);
            StatusLabel.Text = _features.IsInvocable(FeatureIds.ImageTo3D)
                ? "Bild ausgewählt. Mit Asset-Button → Image→3D (TripoSR) starten."
                : "Bild ausgewählt. Image→3D: " + _features.GetStatusMessage(FeatureIds.ImageTo3D);
        }
    }

    private async void BtnAnnyHuman_Click(object sender, RoutedEventArgs e)
    {
        if (!_features.IsInvocable(FeatureIds.AnnyHuman))
        {
            StatusLabel.Text = _features.GetStatusMessage(FeatureIds.AnnyHuman);
            return;
        }
        try
        {
            BtnAnnyHuman.IsEnabled = false;
            StatusLabel.Text = "Anny erzeugt Human…";
            var dest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Generated", $"anny-{DateTime.UtcNow:yyyyMMddHHmmss}.glb");
            var glb = await _anny.GenerateGlbAsync(dest);
            _session.SetActiveMeshFromGlbFile(glb, "anny-body");
            _loadPreview(glb);
            StatusLabel.Text = $"Anny GLB geladen: {glb}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
        finally
        {
            BtnAnnyHuman.IsEnabled = _features.IsInvocable(FeatureIds.AnnyHuman);
        }
    }

    private async void BtnReferenceImage_Click(object sender, RoutedEventArgs e)
    {
        if (!_features.IsInvocable(FeatureIds.ReferenceImageGenerate))
        {
            StatusLabel.Text = _features.GetStatusMessage(FeatureIds.ReferenceImageGenerate);
            return;
        }

        var prompt = string.IsNullOrWhiteSpace(TxtPrompt.Text) ? "character reference sheet" : TxtPrompt.Text.Trim();
        try
        {
            BtnReferenceImage.IsEnabled = false;
            StatusLabel.Text = "FLUX Referenzbild…";
            // Mutate a working bundle, then attach bytes into the authoritative session.
            var working = _session.Snapshot();
            var image = await _referenceImages.GenerateAsync(prompt, seed: null, working);
            if (!working.ReferenceImageBytes.TryGetValue(image.ReferenceImageId, out var png) || png.Length == 0)
                throw new InvalidOperationException("FLUX returned no PNG bytes (honest failure — not status-only).");

            _session.AttachReferenceImage(image, png);
            var tmp = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Generated", $"ref-{image.ReferenceImageId:N}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(tmp)!);
            await File.WriteAllBytesAsync(tmp, png);
            _loadPreview(tmp);
            StatusLabel.Text = $"Referenzbild erzeugt: {tmp} ({image.Width}x{image.Height})";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
            MessageBox.Show(ex.Message, "Referenzbild", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            BtnReferenceImage.IsEnabled = _features.IsInvocable(FeatureIds.ReferenceImageGenerate);
        }
    }

    private async void BtnGeneratePerson_Click(object sender, RoutedEventArgs e)
    {
        if (_features.IsInvocable(FeatureIds.AiGeneratePerson))
        {
            StatusLabel.Text = _features.GetStatusMessage(FeatureIds.AiGeneratePerson);
            return;
        }

        // Allowlisted deterministic AI edit execution (not full person generation).
        if (!_features.IsInvocable(FeatureIds.AiCommandInterpret))
        {
            StatusLabel.Text = _features.GetStatusMessage(FeatureIds.AiCommandInterpret);
            return;
        }

        var prompt = TxtPrompt.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(prompt))
        {
            StatusLabel.Text = "Prompt leer. Beispiele: \"taller, keep head size\", \"gold less shiny\", \"shoulders wider\".";
            return;
        }

        try
        {
            BtnGeneratePerson.IsEnabled = false;
            StatusLabel.Text = "Führe allowlisted AI-Edit aus…";
            var result = await _aiEdits.ExecutePromptAsync(prompt, _commands);
            StatusLabel.Text = $"{result.Status}/{result.Operation}: {result.Message}";
            if (!result.Ok)
                MessageBox.Show(result.Message, "AI-Edit", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
        finally
        {
            BtnGeneratePerson.IsEnabled = true;
        }
    }

    private async void BtnGenerateAsset_Click(object sender, RoutedEventArgs e)
    {
        // Prefer Image→3D when an image is selected and TripoSR is invocable.
        if (!string.IsNullOrWhiteSpace(_selectedImagePath) && File.Exists(_selectedImagePath)
            && _features.IsInvocable(FeatureIds.ImageTo3D))
        {
            try
            {
                BtnGenerateAsset.IsEnabled = false;
                StatusLabel.Text = "Image→3D (TripoSR)…";
                var dest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "3DGod", "Generated", $"triposr-{DateTime.UtcNow:yyyyMMddHHmmss}.glb");
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                var glb = await _imageTo3D.GenerateGlbAsync(_selectedImagePath, dest);
                if (string.IsNullOrWhiteSpace(glb) || !File.Exists(glb))
                    throw new InvalidOperationException("Image→3D wrote no GLB.");
                _session.SetActiveMeshFromGlbFile(glb, "image-to-3d", SourceRepresentation.GeneratedMesh);
                _loadPreview(glb);
                StatusLabel.Text = $"Image→3D GLB: {glb}";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = ex.Message;
                MessageBox.Show(ex.Message, "Image→3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                BtnGenerateAsset.IsEnabled = _features.IsInvocable(FeatureIds.AiGenerateAsset)
                    || _features.IsInvocable(FeatureIds.ImageTo3D);
            }
            return;
        }

        if (!_features.IsInvocable(FeatureIds.AiGenerateAsset))
        {
            StatusLabel.Text = string.IsNullOrWhiteSpace(_selectedImagePath)
                ? "Kein Bild für Image→3D; Asset: " + _features.GetStatusMessage(FeatureIds.AiGenerateAsset)
                : _features.GetStatusMessage(FeatureIds.ImageTo3D);
            return;
        }
        try
        {
            BtnGenerateAsset.IsEnabled = false;
            StatusLabel.Text = "Erzeuge Asset…";
            var asset = await _assets.GenerateAsync(TxtPrompt.Text);
            _session.SetActiveMeshFromGlbFile(asset.GlbPath, asset.Name, SourceRepresentation.GeneratedMesh);
            _loadPreview(asset.GlbPath);
            StatusLabel.Text = $"Asset: {asset.Name} ({asset.Provenance.BackendId})";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
        finally
        {
            BtnGenerateAsset.IsEnabled = _features.IsInvocable(FeatureIds.AiGenerateAsset)
                || _features.IsInvocable(FeatureIds.ImageTo3D);
        }
    }
}
