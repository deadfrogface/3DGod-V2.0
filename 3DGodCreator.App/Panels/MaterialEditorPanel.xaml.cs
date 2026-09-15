using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Application;
using ThreeDGod.Mesh;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class MaterialEditorPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private readonly ActiveProjectSession _session;
    private bool _syncing;

    public MaterialEditorPanel(CharacterSystem cs, ActiveProjectSession session)
    {
        InitializeComponent();
        _cs = cs;
        _session = session;
        CmbMaterial.SelectedIndex = 0;
        CmbPreset.SelectedIndex = 6;
        SyncUiFromSlot();
    }

    private string GetSelectedMaterial()
    {
        return CmbMaterial.SelectedItem is ComboBoxItem item ? item.Content.ToString() ?? "skin" : "skin";
    }

    private void SyncUiFromSlot()
    {
        _syncing = true;
        try
        {
            var matKey = GetSelectedMaterial();
            _cs.ActiveMaterialSlot = matKey;
            var mat = _cs.Materials.GetValueOrDefault(matKey);
            if (mat == null)
                return;
            SldMetallic.Value = mat.Metallic;
            SldRoughness.Value = mat.Roughness;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void PushSlotToCharacterSystem()
    {
        if (_syncing)
            return;
        _cs.SetMaterialPbr(
            GetSelectedMaterial(),
            null,
            SldRoughness.Value,
            SldMetallic.Value);
        SyncToProjectSession();
    }

    private void SyncToProjectSession()
    {
        var key = GetSelectedMaterial();
        if (!_cs.Materials.TryGetValue(key, out var mat))
            return;
        var hex = (mat.Color ?? "#cccccc").Trim().TrimStart('#');
        float r = 0.8f, g = 0.8f, b = 0.8f;
        if (hex.Length >= 6)
        {
            try
            {
                r = Convert.ToInt32(hex[..2], 16) / 255f;
                g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
                b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
            }
            catch { /* keep defaults */ }
        }
        _session.UpsertMaterial(key, r, g, b, 1f, (float)mat.Metallic, (float)mat.Roughness);
    }

    private void CmbMaterial_SelectionChanged(object sender, SelectionChangedEventArgs e) => SyncUiFromSlot();

    private void CmbPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || CmbPreset.SelectedItem is not ComboBoxItem item)
            return;
        var preset = PbrMaterials.Get(item.Content.ToString() ?? "Skin");
        var hex = ColorToHex(preset.BaseColor);
        _cs.SetMaterialPbr(GetSelectedMaterial(), hex, preset.Roughness, preset.Metallic);
        SyncUiFromSlot();
        SyncToProjectSession();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => PushSlotToCharacterSystem();

    private void BtnColor_Click(object sender, RoutedEventArgs e)
    {
        var mat = GetSelectedMaterial();
        var currentHex = _cs.Materials.TryGetValue(mat, out var m) ? m.Color : "#cccccc";
        var dlg = new ColorPickerDialog(currentHex);
        if (dlg.ShowDialog() == true && dlg.SelectedColor is { } color)
        {
            var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            _cs.SetMaterialPbr(mat, hex, null, null);
            SyncToProjectSession();
        }
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        _cs.ActiveMaterialSlot = GetSelectedMaterial();
        PushSlotToCharacterSystem();
        _cs.RefreshLayers();
        SyncToProjectSession();
    }

    private static string ColorToHex(System.Numerics.Vector4 rgba) =>
        $"#{((int)(rgba.X * 255)):X2}{((int)(rgba.Y * 255)):X2}{((int)(rgba.Z * 255)):X2}";
}
