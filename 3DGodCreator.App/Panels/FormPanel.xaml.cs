using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using ThreeDGod.Application;
using ThreeDGodCreator.App;
using ThreeDGodCreator.Core;

namespace ThreeDGodCreator.App.Panels;

public partial class FormPanel : UserControl
{
    private readonly CharacterSystem _cs;
    private readonly ActiveProjectSession _session;
    private readonly Dictionary<string, Slider> _sliders = new();
    private TextBlock? _honestyLabel;

    public FormPanel(CharacterSystem cs, ActiveProjectSession session)
    {
        InitializeComponent();
        _cs = cs;
        _session = session;
        _cs.SliderSyncCallback = RefreshSliders;
        InsertHonestyBanner();
        LoadParameters();
        RefreshHumanCreatorMode();
        _session.Changed += () => Dispatcher.Invoke(RefreshHumanCreatorMode);
    }

    private void InsertHonestyBanner()
    {
        _honestyLabel = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = System.Windows.Media.Brushes.Orange,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 8)
        };
        if (Content is StackPanel root)
            root.Children.Insert(1, _honestyLabel);
    }

    private void RefreshHumanCreatorMode()
    {
        var anny = _session.IsAnnyHumanActive;
        if (_honestyLabel is not null)
        {
            _honestyLabel.Text = anny
                ? "Human Creator = Anny-Tab. Form-Slider sind Legacy und KEINE anatomische Höhe (kein Uniform-Scale als Morph). Nutze Anny-Parameter."
                : "Legacy Form: unskinned Base-GLBs. Größe = nur Viewport-Uniform-Scale (kein Anny-HeightMorph). Für echte Menschen: Anny-Tab.";
        }

        // When Anny human is active, disable Form sculpt to kill dual-state confusion.
        SlidersPanel.IsEnabled = !anny && _cs.IsCurrentModelRigged;
        BtnMale.IsEnabled = !anny;
        BtnFemale.IsEnabled = !anny;
        foreach (var kv in _sliders)
            kv.Value.IsEnabled = SlidersPanel.IsEnabled;
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
                ["height"] = new() { Label = "Größe (Legacy Scale)", Min = 0, Max = 100, Default = 50 },
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
                Width = 140,
                VerticalAlignment = VerticalAlignment.Center
            });
            var slider = new Slider
            {
                Minimum = kv.Value.Min,
                Maximum = kv.Value.Max,
                Value = _cs.SculptData.GetValueOrDefault(kv.Key, kv.Value.Default),
                Width = 180,
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

    public void RefreshModelState()
    {
        RefreshHumanCreatorMode();
    }

    private void BtnMale_Click(object sender, RoutedEventArgs e)
    {
        if (_session.IsAnnyHumanActive)
        {
            MessageBox.Show("Aktives Human-Projekt ist Anny. Form Base-Modelle würden Dual-State erzeugen — abgebrochen.", "Form", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DebugLog.Write("[Form] Männlich gewählt (Legacy base)");
        _cs.SetGender("male");
        _cs.LoadBaseModel("male");
        RefreshSliders();
    }

    private void BtnFemale_Click(object sender, RoutedEventArgs e)
    {
        if (_session.IsAnnyHumanActive)
        {
            MessageBox.Show("Aktives Human-Projekt ist Anny. Form Base-Modelle würden Dual-State erzeugen — abgebrochen.", "Form", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        DebugLog.Write("[Form] Weiblich gewählt (Legacy base)");
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
