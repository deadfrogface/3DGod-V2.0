using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Infrastructure.Components;

namespace ThreeDGodCreator.App.Windows;

public partial class SetupAssistantWindow : Window
{
    private readonly IComponentManager _components;
    private readonly IWorkerUvComponentInstaller? _uvInstaller;
    private bool _showAdvanced;

    public bool Skipped { get; private set; }

    public SetupAssistantWindow(IComponentManager components, IWorkerUvComponentInstaller? uvInstaller = null)
    {
        InitializeComponent();
        _components = components;
        _uvInstaller = uvInstaller;
        Refresh();
    }

    private void Refresh()
    {
        var rows = new ObservableCollection<FeatureRow>();
        foreach (var status in SetupAssistantCatalog.Snapshot(_components))
        {
            rows.Add(new FeatureRow
            {
                Title = status.Feature.Title,
                Description = status.Feature.Description,
                ComponentId = status.Feature.PrimaryComponentId ?? "",
                StateLine = $"{status.State}: {status.Message}",
                AdvancedLine = string.IsNullOrWhiteSpace(status.Feature.AdvancedBackendName)
                    ? ""
                    : $"Backend: {status.Feature.AdvancedBackendName}",
                AdvancedVisibility = _showAdvanced ? Visibility.Visible : Visibility.Collapsed,
                CanInstall = status.CanInstall && !string.IsNullOrWhiteSpace(status.Feature.PrimaryComponentId),
                CanRepair = status.CanRepair,
                CanRemove = status.CanRemove
            });
        }

        FeatureList.ItemsSource = rows;
        LblStatus.Text = "Optional components only. Skip is always safe.";
    }

    private void BtnSkip_Click(object sender, RoutedEventArgs e)
    {
        Skipped = true;
        DialogResult = true;
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void ChkAdvanced_Changed(object sender, RoutedEventArgs e)
    {
        _showAdvanced = ChkAdvanced.IsChecked == true;
        Refresh();
    }

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        => await RunUvAction(sender, "Install");

    private async void BtnRepair_Click(object sender, RoutedEventArgs e)
        => await RunUvAction(sender, "Repair");

    private void BtnRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string componentId } || string.IsNullOrWhiteSpace(componentId))
            return;
        try
        {
            _components.Remove(componentId);
            LblStatus.Text = $"Removed {componentId}.";
            Refresh();
        }
        catch (Exception ex)
        {
            LblStatus.Text = $"Remove failed: {ex.Message}";
        }
    }

    private void BtnDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string componentId } || string.IsNullOrWhiteSpace(componentId))
            return;
        var manifest = _components.ListManifests()
            .FirstOrDefault(m => string.Equals(m.ComponentId, componentId, StringComparison.OrdinalIgnoreCase));
        if (manifest is null)
        {
            LblStatus.Text = $"No manifest for {componentId}.";
            return;
        }

        var diag = _components.Diagnose(manifest);
        LblStatus.Text = $"{diag.ComponentId}: {diag.State} — {diag.Message}";
    }

    private async Task RunUvAction(object sender, string label)
    {
        if (sender is not Button { Tag: string componentId } || string.IsNullOrWhiteSpace(componentId))
            return;
        if (_uvInstaller is null)
        {
            LblStatus.Text = "Installer not registered.";
            return;
        }

        try
        {
            LblStatus.Text = $"{label} {componentId}…";
            var progress = new Progress<string>(msg => LblStatus.Text = msg);
            var result = await _uvInstaller.InstallOrRepairAsync(componentId, acceptLicense: true, progress);
            LblStatus.Text = $"{label} {componentId}: {result.State} — {result.Message}";
            Refresh();
        }
        catch (Exception ex)
        {
            LblStatus.Text = $"{label} failed: {ex.Message}";
        }
    }

    private sealed class FeatureRow
    {
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public string ComponentId { get; init; } = "";
        public string StateLine { get; init; } = "";
        public string AdvancedLine { get; init; } = "";
        public Visibility AdvancedVisibility { get; init; }
        public bool CanInstall { get; init; }
        public bool CanRepair { get; init; }
        public bool CanRemove { get; init; }
    }
}
