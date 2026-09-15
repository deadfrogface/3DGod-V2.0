using System.IO;
using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class ClothingPanel : UserControl
{
    private readonly IFeatureAvailabilityService _features;
    private readonly IGarmentFitService _garmentFit;
    private readonly ActiveProjectSession _session;
    private readonly Action<string> _loadPreview;

    public ClothingPanel(
        CharacterSystem cs,
        IFeatureAvailabilityService features,
        IGarmentFitService garmentFit,
        ActiveProjectSession session,
        Action<string> loadPreview)
    {
        InitializeComponent();
        _ = cs;
        _features = features;
        _garmentFit = garmentFit;
        _session = session;
        _loadPreview = loadPreview;

        var fitMsg = _features.GetStatusMessage(FeatureIds.ClothingFit);
        var fitOk = _features.IsInvocable(FeatureIds.ClothingFit);
        AvailabilityLabel.Text =
            fitMsg + "\nPiercings/Tattoos: NotImplemented – no mesh loaders. Buttons stay disabled.";

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

    private async void BtnClothes_Click(object sender, RoutedEventArgs e)
    {
        if (!_features.IsInvocable(FeatureIds.ClothingFit))
        {
            MessageBox.Show(_features.GetStatusMessage(FeatureIds.ClothingFit), "Unavailable", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            BtnClothes.IsEnabled = false;
            AvailabilityLabel.Text = "Fitting jacket via GarmentCode + geometry3Sharp…";

            var work = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Garments");
            Directory.CreateDirectory(work);

            var body = _session.GetActiveMeshGlbPathOrMaterialize(Path.Combine(work, "body"));
            GarmentFitResult result;
            if (!string.IsNullOrWhiteSpace(body) && File.Exists(body))
                result = await _garmentFit.FitJacketToBodyAsync(body, work, "active-human");
            else
                result = await _garmentFit.FitJacketToPresetAsync("male_base", work);

            if (string.IsNullOrWhiteSpace(result.FittedGlb) || !File.Exists(result.FittedGlb))
                throw new InvalidOperationException("Fit produced no GLB (honest failure — no fake clothing).");

            _session.AddFittedGarment(result.FittedGlb, "jacket", result.Report);
            _loadPreview(result.FittedGlb);
            AvailabilityLabel.Text =
                $"Jacket fitted. insideAfter={result.Report.InsideAfter}, minDist={result.Report.MinDistanceAfter:F4}. Persisted in project session.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Clothing Fit", MessageBoxButton.OK, MessageBoxImage.Warning);
            AvailabilityLabel.Text = ex.Message;
        }
        finally
        {
            BtnClothes.IsEnabled = _features.IsInvocable(FeatureIds.ClothingFit);
        }
    }

    private void BtnPiercings_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("NotImplemented – no piercing mesh pipeline.", "Unavailable", MessageBoxButton.OK, MessageBoxImage.Information);

    private void BtnTattoos_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("NotImplemented – no tattoo pipeline.", "Unavailable", MessageBoxButton.OK, MessageBoxImage.Information);
}
