using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGod.Application;
using ThreeDGod.Workers;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class AiPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private readonly IFeatureAvailabilityService _features;
    private readonly AnnyHumanService _anny;
    private readonly Action<string> _loadPreview;
    private string? _selectedImagePath;

    public AiPanel(CharacterSystem cs, IFeatureAvailabilityService features, AnnyHumanService anny, Action<string> loadPreview)
    {
        InitializeComponent();
        _cs = cs;
        _features = features;
        _anny = anny;
        _loadPreview = loadPreview;
        AvailabilityLabel.Text = _features.GetStatusMessage(FeatureIds.AnnyHuman);
        var annyOk = _features.IsInvocable(FeatureIds.AnnyHuman);
        var personOk = _features.IsInvocable(FeatureIds.AiGeneratePerson);
        var assetOk = _features.IsInvocable(FeatureIds.AiGenerateAsset);
        BtnAnnyHuman.IsEnabled = annyOk;
        BtnReferenceImage.IsEnabled = _features.IsInvocable(FeatureIds.ReferenceImageGenerate);
        BtnGeneratePerson.IsEnabled = personOk;
        BtnGenerateAsset.IsEnabled = assetOk;
        TxtPrompt.IsEnabled = personOk || assetOk;
        StatusLabel.Text = annyOk
            ? _features.GetStatusMessage(FeatureIds.AnnyHuman)
            : "Anny NotInstalled/NotImplemented – Prompt-Person und Asset erzeugen kein Mesh.";
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
            LblImage.Text = System.IO.Path.GetFileName(_selectedImagePath);
            StatusLabel.Text = "Bild ausgewählt. Generierung ist trotzdem nicht implementiert.";
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

    private void BtnReferenceImage_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Text = _features.GetStatusMessage(FeatureIds.ReferenceImageGenerate);
    }

    private void BtnGeneratePerson_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Text = _features.GetStatusMessage(FeatureIds.AiGeneratePerson);
    }

    private void BtnGenerateAsset_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Text = _features.GetStatusMessage(FeatureIds.AiGenerateAsset);
    }
}
