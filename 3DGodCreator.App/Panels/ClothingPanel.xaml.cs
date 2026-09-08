using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Application;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class ClothingPanel : UserControl
{
    private readonly IFeatureAvailabilityService _features;

    public ClothingPanel(CharacterSystem cs, IFeatureAvailabilityService features)
    {
        InitializeComponent();
        _ = cs;
        _features = features;
        var ok = _features.IsInvocable(FeatureIds.ClothingFit);
        BtnClothes.IsEnabled = ok;
        BtnPiercings.IsEnabled = ok;
        BtnTattoos.IsEnabled = ok;
        AvailabilityLabel.Text = _features.GetStatusMessage(FeatureIds.ClothingFit);
    }

    private void BtnClothes_Click(object sender, RoutedEventArgs e) => ShowUnavailable();
    private void BtnPiercings_Click(object sender, RoutedEventArgs e) => ShowUnavailable();
    private void BtnTattoos_Click(object sender, RoutedEventArgs e) => ShowUnavailable();

    private void ShowUnavailable() =>
        MessageBox.Show(_features.GetStatusMessage(FeatureIds.ClothingFit), "NotImplemented", MessageBoxButton.OK, MessageBoxImage.Information);
}
