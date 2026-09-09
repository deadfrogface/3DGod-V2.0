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
        _ = features.IsInvocable(FeatureIds.PhysicsSimulate);
        ChkBreasts.IsEnabled = false;
        ChkCloth.IsEnabled = false;
        ChkPiercing.IsEnabled = false;
    }

    private void OnChanged(object sender, RoutedEventArgs e)
    {
        // No live WPF preview. Chain/earring exist via IAccessoryPhysicsService. Not cloth.
    }
}
