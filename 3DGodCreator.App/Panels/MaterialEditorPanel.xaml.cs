using System.Windows;
using System.Windows.Controls;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class MaterialEditorPanel : UserControl
{
    private readonly CharacterSystem _cs;

    public MaterialEditorPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
        CmbMaterial.SelectedIndex = 0;
    }

    private string GetSelectedMaterial()
    {
        return CmbMaterial.SelectedItem is ComboBoxItem item ? item.Content.ToString() ?? "skin" : "skin";
    }

    private void CmbMaterial_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void BtnColor_Click(object sender, RoutedEventArgs e)
    {
        var mat = GetSelectedMaterial();
        var currentHex = _cs.Materials.TryGetValue(mat, out var m) ? m.Color : "#cccccc";
        var dlg = new ColorPickerDialog(currentHex);
        if (dlg.ShowDialog() == true && dlg.SelectedColor is { } color)
        {
            var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            _cs.SetMaterialColor(mat, hex);
        }
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        _cs.RefreshLayers();
    }
}
