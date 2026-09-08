using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Application;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class PhysicsPanel : UserControl
{
    public PhysicsPanel(CharacterSystem cs, IFeatureAvailabilityService features)
    {
        InitializeComponent();
        _ = cs;
        AvailabilityLabel.Text = features.GetStatusMessage(FeatureIds.PhysicsSimulate);
        var ok = features.IsInvocable(FeatureIds.PhysicsSimulate);
        ChkBreasts.IsEnabled = ok;
        ChkCloth.IsEnabled = ok;
        ChkPiercing.IsEnabled = ok;
    }

    private void OnChanged(object sender, RoutedEventArgs e)
    {
        // Intentionally no-op while physics is NotImplemented.
    }
}
