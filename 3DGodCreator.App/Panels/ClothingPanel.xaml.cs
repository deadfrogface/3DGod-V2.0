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

    private void BtnAddClothes_Click(object sender, RoutedEventArgs e)
    {
        _cs.AddAsset("clothes");
        _cs.RefreshLayers();
    }
}
