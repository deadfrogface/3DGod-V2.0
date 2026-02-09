using System.Windows;
using System.Windows.Controls;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class ClothingPanel : UserControl
{
    private readonly CharacterSystem _cs;

    public ClothingPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
    }

    private void BtnClothes_Click(object sender, RoutedEventArgs e)
    {
        _cs.AddAsset("clothes");
        _cs.RefreshLayers();
    }

    private void BtnPiercings_Click(object sender, RoutedEventArgs e)
    {
        _cs.AddAsset("piercings");
        _cs.RefreshLayers();
    }

    private void BtnTattoos_Click(object sender, RoutedEventArgs e)
    {
        _cs.AddAsset("tattoos");
        _cs.RefreshLayers();
    }
}
