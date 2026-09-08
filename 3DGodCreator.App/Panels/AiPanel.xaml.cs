using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGod.Application;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class AiPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private readonly IFeatureAvailabilityService _features;
    private string? _selectedImagePath;

    public AiPanel(CharacterSystem cs, IFeatureAvailabilityService features)
    {
        InitializeComponent();
        _cs = cs;
        _features = features;
        AvailabilityLabel.Text = _features.GetStatusMessage(FeatureIds.AiGeneratePerson);
        var personOk = _features.IsInvocable(FeatureIds.AiGeneratePerson);
        var assetOk = _features.IsInvocable(FeatureIds.AiGenerateAsset);
        BtnGeneratePerson.IsEnabled = personOk;
        BtnGenerateAsset.IsEnabled = assetOk;
        TxtPrompt.IsEnabled = personOk || assetOk;
        StatusLabel.Text = personOk ? "" : "NotImplemented – es wird kein Mesh erzeugt.";
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

    private void BtnGeneratePerson_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Text = _features.GetStatusMessage(FeatureIds.AiGeneratePerson);
    }

    private void BtnGenerateAsset_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Text = _features.GetStatusMessage(FeatureIds.AiGenerateAsset);
    }
}
