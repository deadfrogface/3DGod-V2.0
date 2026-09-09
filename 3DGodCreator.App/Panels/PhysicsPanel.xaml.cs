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
        AvailabilityLabel.Text = features.GetStatusMessage(FeatureIds.PhysicsSimulate)
            + "\nUI checkboxes below are informational only (disabled). Live preview = Bepu rigid accessory chain/earring in tests/services, not cloth/softbody.";

        ChkBreasts.Content = "Brust Softbody (Unavailable – NotImplemented)";
        ChkCloth.Content = "Stoffsimulation (Unavailable – NotImplemented)";
        ChkPiercing.Content = "Piercing Softbody (Unavailable) – Bepu earring chain is separate";

        ChkBreasts.IsEnabled = false;
        ChkCloth.IsEnabled = false;
        ChkPiercing.IsEnabled = false;
    }

    private void OnChanged(object sender, RoutedEventArgs e)
    {
        // Checkboxes stay disabled. No live cloth/softbody preview.
    }
}
