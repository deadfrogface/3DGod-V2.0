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

        var fitMsg = _features.GetStatusMessage(FeatureIds.ClothingFit);
        var fitOk = _features.IsInvocable(FeatureIds.ClothingFit);
        AvailabilityLabel.Text =
            fitMsg + "\nPiercings/Tattoos: NotImplemented – no mesh loaders. Buttons stay disabled.";

        // Jacket fit may be Experimental when GarmentCode is installed; piercings/tattoos are never real.
        BtnClothes.IsEnabled = fitOk;
        BtnClothes.Content = fitOk ? "Jacket Fit (Experimental)" : "Lade Kleidung (Unavailable)";
        BtnClothes.ToolTip = fitMsg;

        BtnPiercings.IsEnabled = false;
        BtnPiercings.Content = "Lade Piercings (Unavailable)";
        BtnPiercings.ToolTip = "NotImplemented – no piercing mesh pipeline.";

        BtnTattoos.IsEnabled = false;
        BtnTattoos.Content = "Lade Tattoos (Unavailable)";
        BtnTattoos.ToolTip = "NotImplemented – no tattoo pipeline.";
    }

    private void BtnClothes_Click(object sender, RoutedEventArgs e)
    {
        if (!_features.IsInvocable(FeatureIds.ClothingFit))
        {
            MessageBox.Show(_features.GetStatusMessage(FeatureIds.ClothingFit), "Unavailable", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(
            "Experimental – Jacket fit runs via GarmentCode + geometry3Sharp when invoked from Anny/Garment tools. This panel does not fake a clothing load.",
            "Clothing",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnPiercings_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("NotImplemented – no piercing mesh pipeline.", "Unavailable", MessageBoxButton.OK, MessageBoxImage.Information);

    private void BtnTattoos_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("NotImplemented – no tattoo pipeline.", "Unavailable", MessageBoxButton.OK, MessageBoxImage.Information);
}
