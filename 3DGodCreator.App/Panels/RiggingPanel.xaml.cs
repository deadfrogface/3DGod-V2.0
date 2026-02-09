using System.Windows;
using System.Windows.Controls;
using ThreeDGodCreator.App;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class RiggingPanel : UserControl
{
    private readonly CharacterSystem _cs;

    public RiggingPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
    }

    private void BtnAutoRig_Click(object sender, RoutedEventArgs e)
    {
        DebugLog.Write("[Rigging] Starte Auto-Rig...");
        _cs.CreateAutoRig();
    }

    private void BtnMetahuman_Click(object sender, RoutedEventArgs e)
    {
        DebugLog.Write("[Rigging] Exportiere Metahuman-Rig...");
        _cs.ExportFbx("metahuman_rigged");
    }
}
