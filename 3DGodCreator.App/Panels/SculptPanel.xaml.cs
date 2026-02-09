using System.Windows;
using System.Windows.Controls;
using ThreeDGodCreator.App;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class SculptPanel : UserControl
{
    private readonly CharacterSystem _cs;

    public SculptPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
        ChkSymmetry.IsChecked = _cs.SculptData.GetValueOrDefault("symmetry", 1) == 1;
        ChkSymmetry.Checked += (_, _) => _cs.SculptData["symmetry"] = 1;
        ChkSymmetry.Unchecked += (_, _) => _cs.SculptData["symmetry"] = 0;
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Text = "Sculpting wird geladen...";
        DebugLog.Write("[Sculpt] Starte Blender Sculpting...");
        _cs.Sculpt();
        StatusLabel.Text = "Sculpting-Modus aktiv (in Blender)";
    }
}
