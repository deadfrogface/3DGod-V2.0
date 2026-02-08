using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class AiPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private string? _selectedImagePath;

    public AiPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
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
            StatusLabel.Text = "Bild ausgewählt.";
        }
    }

    private void BtnGeneratePerson_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Text = "KI-Generierung (Phase 2: ONNX-Service nicht implementiert). Bild laden und TripoSR-Python-Backend nutzen.";
    }

    private void BtnGenerateAsset_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Text = "KI-Asset-Generierung (Phase 2: ONNX-Service nicht implementiert).";
    }
}
