using System.Windows;
using System.Windows.Controls;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class NsfwPanel : UserControl
{
    private readonly CharacterSystem _cs;

    public NsfwPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
        ChkBreasts.IsChecked = _cs.AnatomyState.GetValueOrDefault("breasts", true);
        ChkGenitals.IsChecked = _cs.AnatomyState.GetValueOrDefault("genitals", true);
    }

    private void OnLayerChanged(object sender, RoutedEventArgs e)
    {
        _cs.AnatomyState["breasts"] = ChkBreasts.IsChecked == true;
        _cs.AnatomyState["genitals"] = ChkGenitals.IsChecked == true;
        _cs.RefreshLayers();
    }
}
