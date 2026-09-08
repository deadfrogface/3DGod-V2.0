using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Application;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class RiggingPanel : UserControl
{
    private readonly IFeatureAvailabilityService _features;

    public RiggingPanel(CharacterSystem cs, IFeatureAvailabilityService features)
    {
        InitializeComponent();
        _ = cs;
        _features = features;
        AvailabilityLabel.Text = _features.GetStatusMessage(FeatureIds.RigAuto);
        BtnAutoRig.IsEnabled = _features.IsInvocable(FeatureIds.RigAuto);
        BtnMetahuman.IsEnabled = _features.IsInvocable(FeatureIds.ExportMetahuman);
    }

    private void BtnAutoRig_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(_features.GetStatusMessage(FeatureIds.RigAuto), "NotImplemented", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnMetahuman_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(_features.GetStatusMessage(FeatureIds.ExportMetahuman), "NotImplemented", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
