using System.IO;
using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Application;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class RiggingPanel : UserControl
{
    private readonly IFeatureAvailabilityService _features;
    private readonly IAutoRigService _autoRig;
    private readonly ActiveProjectSession _session;
    private readonly Action<string> _loadPreview;

    public RiggingPanel(
        CharacterSystem cs,
        IFeatureAvailabilityService features,
        IAutoRigService autoRig,
        ActiveProjectSession session,
        Action<string> loadPreview)
    {
        InitializeComponent();
        _ = cs;
        _features = features;
        _autoRig = autoRig;
        _session = session;
        _loadPreview = loadPreview;

        RefreshAvailabilityUi();
        BtnMetahuman.IsEnabled = false;
        BtnMetahuman.Content = "MetaHuman (Unavailable)";
        BtnMetahuman.ToolTip = _features.GetStatusMessage(FeatureIds.ExportMetahuman);
    }

    private void RefreshAvailabilityUi()
    {
        var selection = _autoRig.DescribeSelection(ReadPreference());
        var autoOk = _features.IsInvocable(FeatureIds.RigAuto);
        AvailabilityLabel.Text =
            "Anny humans: parametric rig profile in Anny tab.\n" +
            "Freeform / imported meshes: multi-backend Auto-Rig (CPU / Vulkan / CUDA).\n" +
            selection.Reason;

        BtnAutoRig.IsEnabled = autoOk && selection.Selected is not null;
        BtnAutoRig.Content = selection.Selected is null
            ? "Auto-Rig (Unavailable)"
            : $"Auto-Rig ({selection.Selected.DisplayName ?? selection.Selected.ProviderId})";
        BtnAutoRig.ToolTip = selection.Reason;
    }

    private AutoRigDevicePreference ReadPreference()
    {
        if (CmbProvider.SelectedItem is ComboBoxItem { Tag: string tag })
        {
            return tag switch
            {
                "Cpu" => AutoRigDevicePreference.Cpu,
                "Vulkan" => AutoRigDevicePreference.Vulkan,
                "NvidiaCuda" => AutoRigDevicePreference.NvidiaCuda,
                _ => AutoRigDevicePreference.Automatic
            };
        }
        return AutoRigDevicePreference.Automatic;
    }

    private async void BtnAutoRig_Click(object sender, RoutedEventArgs e)
    {
        var preference = ReadPreference();
        if (!_features.IsInvocable(FeatureIds.RigAuto))
        {
            MessageBox.Show(_features.GetStatusMessage(FeatureIds.RigAuto), "Auto-Rig", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            BtnAutoRig.IsEnabled = false;
            AvailabilityLabel.Text = $"Auto-Rig ({preference})…";

            var work = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Rigging");
            Directory.CreateDirectory(work);
            var src = _session.GetActiveMeshGlbPathOrMaterialize(work);
            if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
                throw new InvalidOperationException("No active project mesh to rig. Generate/import a body first.");

            var dest = Path.Combine(work, $"autoroot-{DateTime.UtcNow:yyyyMMddHHmmss}.glb");
            var result = await _autoRig.RigAsync(src, dest, preference);
            if (string.IsNullOrWhiteSpace(result.OutputGlb) || !File.Exists(result.OutputGlb))
                throw new InvalidOperationException("Auto-Rig produced no GLB (honest failure).");

            _session.SetActiveRigFromGlb(result.OutputGlb, result.ProviderId);
            _loadPreview(result.OutputGlb);
            AvailabilityLabel.Text =
                $"Auto-Rig OK via {result.ProviderId} ({result.Device}). Provenance={result.Provenance}. Path={result.OutputGlb}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Auto-Rig", MessageBoxButton.OK, MessageBoxImage.Warning);
            AvailabilityLabel.Text = ex.Message;
        }
        finally
        {
            RefreshAvailabilityUi();
        }
    }

    private void BtnMetahuman_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show(_features.GetStatusMessage(FeatureIds.ExportMetahuman), "NotImplemented", MessageBoxButton.OK, MessageBoxImage.Information);
}
