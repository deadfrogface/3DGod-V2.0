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
        ChkSkin.IsChecked = _cs.AnatomyState.GetValueOrDefault("skin", true);
        ChkFat.IsChecked = _cs.AnatomyState.GetValueOrDefault("fat", true);
        ChkMuscle.IsChecked = _cs.AnatomyState.GetValueOrDefault("muscle", false);
        ChkBone.IsChecked = _cs.AnatomyState.GetValueOrDefault("bone", false);
        ChkOrgans.IsChecked = _cs.AnatomyState.GetValueOrDefault("organs", false);
        ChkBreasts.IsChecked = _cs.AnatomyState.GetValueOrDefault("breasts", true);
        ChkGenitals.IsChecked = _cs.AnatomyState.GetValueOrDefault("genitals", true);
        ChkBodyhair.IsChecked = _cs.AnatomyState.GetValueOrDefault("bodyhair", false);
    }

    private void OnAnatomyChanged(object sender, RoutedEventArgs e)
    {
        _cs.AnatomyState["skin"] = ChkSkin.IsChecked == true;
        _cs.AnatomyState["fat"] = ChkFat.IsChecked == true;
        _cs.AnatomyState["muscle"] = ChkMuscle.IsChecked == true;
        _cs.AnatomyState["bone"] = ChkBone.IsChecked == true;
        _cs.AnatomyState["organs"] = ChkOrgans.IsChecked == true;
        _cs.RefreshLayers();
    }

    private void OnLayerChanged(object sender, RoutedEventArgs e)
    {
        _cs.AnatomyState["breasts"] = ChkBreasts.IsChecked == true;
        _cs.AnatomyState["genitals"] = ChkGenitals.IsChecked == true;
        _cs.AnatomyState["bodyhair"] = ChkBodyhair.IsChecked == true;
        _cs.RefreshLayers();
    }
}
