using System.Collections.Generic;
using System.IO;
using ThreeDGodCreator.App;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class FormPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private readonly Dictionary<string, Slider> _sliders = new();

    public FormPanel(CharacterSystem cs)
    {
        InitializeComponent();
        _cs = cs;
        _cs.SliderSyncCallback = RefreshSliders;
        LoadParameters();
    }

    private void LoadParameters()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "body_parameters.json");

        Dictionary<string, BodyParam>? pars = null;
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                pars = JsonSerializer.Deserialize<Dictionary<string, BodyParam>>(json);
            }
            catch { }
        }

        if (pars == null)
        {
            pars = new Dictionary<string, BodyParam>
            {
                ["height"] = new() { Label = "Größe", Min = 0, Max = 100, Default = 50 },
                ["breast_size"] = new() { Label = "Brustgröße", Min = 0, Max = 100, Default = 50 },
                ["hip_width"] = new() { Label = "Hüftbreite", Min = 0, Max = 100, Default = 50 },
                ["arm_length"] = new() { Label = "Armlänge", Min = 0, Max = 100, Default = 50 },
                ["leg_length"] = new() { Label = "Beinlänge", Min = 0, Max = 100, Default = 50 }
            };
        }

        foreach (var kv in pars)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            sp.Children.Add(new TextBlock
            {
                Text = kv.Value.Label,
                Foreground = System.Windows.Media.Brushes.White,
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center
            });
            var slider = new Slider
            {
                Minimum = kv.Value.Min,
                Maximum = kv.Value.Max,
                Value = _cs.SculptData.GetValueOrDefault(kv.Key, kv.Value.Default),
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };
            var key = kv.Key;
            slider.ValueChanged += (_, _) =>
            {
                _cs.UpdateSculptValue(key, (int)slider.Value);
            };
            _sliders[key] = slider;
            sp.Children.Add(slider);
            SlidersPanel.Children.Add(sp);
        }
    }

    public void RefreshSliders()
    {
        foreach (var kv in _sliders)
        {
            if (_cs.SculptData.TryGetValue(kv.Key, out var v) && Math.Abs(kv.Value.Value - v) > 0.01)
                kv.Value.Value = v;
        }
    }

    private void BtnMale_Click(object sender, RoutedEventArgs e)
    {
        DebugLog.Write("[Form] Männlich gewählt");
        _cs.SetGender("male");
        _cs.LoadBaseModel("male");
        RefreshSliders();
    }

    private void BtnFemale_Click(object sender, RoutedEventArgs e)
    {
        DebugLog.Write("[Form] Weiblich gewählt");
        _cs.SetGender("female");
        _cs.LoadBaseModel("female");
        RefreshSliders();
    }

    private class BodyParam
    {
        [JsonPropertyName("label")] public string Label { get; set; } = "";
        [JsonPropertyName("min")] public int Min { get; set; }
        [JsonPropertyName("max")] public int Max { get; set; }
        [JsonPropertyName("default")] public int Default { get; set; }
    }
}
