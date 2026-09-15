using System.IO;
using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Application;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class RiggingPanel : UserControl
{
    private readonly IFeatureAvailabilityService _features;
    private readonly ISkinTokensRigService _skinTokens;
    private readonly ActiveProjectSession _session;
    private readonly Action<string> _loadPreview;

    public RiggingPanel(
        CharacterSystem cs,
        IFeatureAvailabilityService features,
        ISkinTokensRigService skinTokens,
        ActiveProjectSession session,
        Action<string> loadPreview)
    {
        InitializeComponent();
        _ = cs;
        _features = features;
        _skinTokens = skinTokens;
        _session = session;
        _loadPreview = loadPreview;

        var skinMsg = _features.GetStatusMessage(FeatureIds.SkinTokens);
        var skinOk = _features.IsInvocable(FeatureIds.SkinTokens);
        AvailabilityLabel.Text =
            "Anny humans: inspect parametric rig profile in Anny tab.\n" +
            "Freeform / imported meshes: SkinTokens auto-rig when hardware allows.\n" +
            skinMsg;

        BtnAutoRig.IsEnabled = skinOk;
        BtnAutoRig.Content = skinOk ? "Auto-Rig (SkinTokens)" : "Auto-Rig (Unavailable)";
        BtnAutoRig.ToolTip = skinMsg;

        BtnMetahuman.IsEnabled = false;
        BtnMetahuman.Content = "MetaHuman (Unavailable)";
        BtnMetahuman.ToolTip = _features.GetStatusMessage(FeatureIds.ExportMetahuman);
    }

    private async void BtnAutoRig_Click(object sender, RoutedEventArgs e)
    {
        if (!_features.IsInvocable(FeatureIds.SkinTokens))
        {
            MessageBox.Show(_features.GetStatusMessage(FeatureIds.SkinTokens), "SkinTokens", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            BtnAutoRig.IsEnabled = false;
            AvailabilityLabel.Text = "SkinTokens auto-rig…";

            var work = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Rigging");
            Directory.CreateDirectory(work);
            var src = _session.GetActiveMeshGlbPathOrMaterialize(work);
            if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
                throw new InvalidOperationException("No active project mesh to rig. Generate/import a body first.");

            var dest = Path.Combine(work, $"skintokens-{DateTime.UtcNow:yyyyMMddHHmmss}.glb");
            var rigged = await _skinTokens.RigGlbAsync(src, dest);
            if (string.IsNullOrWhiteSpace(rigged) || !File.Exists(rigged))
                throw new InvalidOperationException("SkinTokens produced no GLB.");

            _session.SetActiveRigFromGlb(rigged, "skintokens");
            _loadPreview(rigged);
            AvailabilityLabel.Text = $"SkinTokens rig written: {rigged}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Auto-Rig", MessageBoxButton.OK, MessageBoxImage.Warning);
            AvailabilityLabel.Text = ex.Message;
        }
        finally
        {
            BtnAutoRig.IsEnabled = _features.IsInvocable(FeatureIds.SkinTokens);
        }
    }

    private void BtnMetahuman_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(_features.GetStatusMessage(FeatureIds.ExportMetahuman), "NotImplemented", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
