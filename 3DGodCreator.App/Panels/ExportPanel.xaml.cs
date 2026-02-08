using System.Windows;
using System.Windows.Controls;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class ExportPanel : UserControl
{
    private readonly CharacterSystem _cs;

    public ExportPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtFilename.Text.Trim();
        if (string.IsNullOrEmpty(name)) name = "exported_character";
        _cs.ExportFbx(name);
    }
}
