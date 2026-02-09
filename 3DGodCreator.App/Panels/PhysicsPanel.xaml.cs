using System.Windows;
using System.Windows.Controls;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class PhysicsPanel : UserControl
{
    private readonly CharacterSystem _cs;

    public PhysicsPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
        ChkBreasts.IsChecked = _cs.PhysicsFlags.GetValueOrDefault("breasts", true);
        ChkCloth.IsChecked = _cs.PhysicsFlags.GetValueOrDefault("cloth", true);
        ChkPiercing.IsChecked = _cs.PhysicsFlags.GetValueOrDefault("piercings", true);
    }

    private void OnChanged(object sender, RoutedEventArgs e)
    {
        _cs.PhysicsFlags["breasts"] = ChkBreasts.IsChecked == true;
        _cs.PhysicsFlags["cloth"] = ChkCloth.IsChecked == true;
        _cs.PhysicsFlags["piercings"] = ChkPiercing.IsChecked == true;
    }
}
