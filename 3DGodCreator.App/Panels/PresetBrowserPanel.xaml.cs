using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ThreeDGodCreator.App;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class PresetBrowserPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private readonly string _presetPath;

    public PresetBrowserPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
        _presetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets");
        RefreshList();
    }

    private void RefreshList()
    {
        PresetList.Items.Clear();
        if (!Directory.Exists(_presetPath))
        {
            Directory.CreateDirectory(_presetPath);
            return;
        }
        foreach (var file in Directory.GetFiles(_presetPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var name = Path.GetFileNameWithoutExtension(file);
                var nsfw = data.TryGetProperty("nsfw", out var n) && n.GetBoolean();
                PresetList.Items.Add($"{(nsfw ? "🔞" : "🟢")} {name}");
            }
            catch { }
        }
    }

    private void PresetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetList.SelectedItem is string item)
        {
            var name = item.Replace("🔞", "").Replace("🟢", "").Trim();
            DebugLog.Write($"[Presets] Lade Preset: {name}");
            _cs.LoadPreset(name);
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshList();
    }

    private void BtnScreenshot_Click(object sender, RoutedEventArgs e)
    {
        if (PresetList.SelectedItem is not string item)
        {
            MessageBox.Show("Kein Preset ausgewählt.", "Screenshot", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var name = item.Replace("🔞", "").Replace("🟢", "").Trim();
        var targetPath = Path.Combine(_presetPath, $"{name}.jpg");
        try
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                var rtb = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using var fs = File.Create(targetPath);
                encoder.Save(fs);
                DebugLog.Write($"[Presets] Screenshot gespeichert: {targetPath}");
                MessageBox.Show($"Screenshot gespeichert: {targetPath}", "Screenshot", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler: {ex.Message}", "Screenshot", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
