using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Workers;

namespace ThreeDGodCreator.App.Panels;

public partial class AnnyInspectorPanel : UserControl
{
    private readonly AnnyHumanService _anny;
    private readonly CommandStack _commands;
    private readonly Action<string> _loadPreview;
    private readonly DispatcherTimer _debounce;
    private readonly Dictionary<string, Slider> _sliders = new();
    private readonly Guid _targetId = Guid.NewGuid();
    private bool _suppress;
    private bool _busy;

    public ParametricHumanState State { get; private set; } = new()
    {
        BackendId = "anny",
        TopologyProfile = "anny",
        RigProfile = "anny"
    };

    public AnnyInspectorPanel(AnnyHumanService anny, IFeatureAvailabilityService features, CommandStack commands, Action<string> loadPreview)
    {
        InitializeComponent();
        _anny = anny;
        _commands = commands;
        _loadPreview = loadPreview;
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _debounce.Tick += async (_, _) =>
        {
            _debounce.Stop();
            await GeneratePreviewAsync();
        };

        if (!features.IsInvocable(FeatureIds.AnnyHuman))
        {
            StatusLabel.Text = features.GetStatusMessage(FeatureIds.AnnyHuman);
            IsEnabled = false;
            return;
        }

        StatusLabel.Text = features.GetStatusMessage(FeatureIds.AnnyHuman);
        Loaded += async (_, _) => await LoadCatalogAsync();
    }

    public async Task ApplyStateAsync(ParametricHumanState state, bool generate)
    {
        State = Clone(state);
        _suppress = true;
        foreach (var kv in _sliders)
        {
            if (TryGet(State, kv.Key, out var value))
                kv.Value.Value = value;
        }
        _suppress = false;
        if (generate)
            await GeneratePreviewAsync();
    }

    private async Task LoadCatalogAsync()
    {
        try
        {
            StatusLabel.Text = "Lade Anny-Catalog…";
            var catalog = await _anny.GetCatalogAsync();
            BuildSliders(catalog);
            StatusLabel.Text = $"Catalog: {catalog.PhenotypeKeys.Count} Körper, {catalog.LocalChangeKeys.Count} Local, {catalog.FacialActionKeys.Count} Gesicht.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    private void BuildSliders(AnnyCatalog catalog)
    {
        SlidersHost.Children.Clear();
        _sliders.Clear();
        AddGroup("Körper", catalog.PhenotypeKeys, 0, 1, 0.5f, "body");
        AddGroup("Local Shape", catalog.LocalChangeKeys.Take(16).ToArray(), -1, 1, 0f, "local");
        AddGroup("Gesicht", catalog.FacialActionKeys.Take(16).ToArray(), 0, 1, 0f, "face");
    }

    private void AddGroup(string title, IReadOnlyList<string> keys, double min, double max, float fallback, string category)
    {
        SlidersHost.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = System.Windows.Media.Brushes.White,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 8, 0, 4)
        });
        foreach (var key in keys)
        {
            if (!_sliders.ContainsKey(category + ":" + key) && !TryGet(State, category + ":" + key, out _))
                SetValue(State, category + ":" + key, fallback);
            var current = TryGet(State, category + ":" + key, out var stored) ? stored : fallback;
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            row.Children.Add(new TextBlock
            {
                Text = key,
                Foreground = System.Windows.Media.Brushes.LightGray,
                Width = 180,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            });
            var slider = new Slider { Minimum = min, Maximum = max, Value = current, Width = 180 };
            var id = category + ":" + key;
            float dragStart = current;
            slider.PreviewMouseLeftButtonDown += (_, _) => dragStart = (float)slider.Value;
            slider.ValueChanged += (_, _) =>
            {
                if (_suppress) return;
                SetValue(State, id, (float)slider.Value);
                _debounce.Stop();
                _debounce.Start();
            };
            slider.PreviewMouseLeftButtonUp += (_, _) => Commit(id, dragStart, (float)slider.Value);
            _sliders[id] = slider;
            row.Children.Add(slider);
            SlidersHost.Children.Add(row);
        }
    }

    private void Commit(string id, float oldValue, float newValue)
    {
        if (Math.Abs(oldValue - newValue) < 0.0001f)
            return;
        var command = new PropertyChangeCommand(
            _targetId,
            id,
            oldValue,
            newValue,
            value =>
            {
                SetValue(State, id, Convert.ToSingle(value));
                _suppress = true;
                if (_sliders.TryGetValue(id, out var slider))
                    slider.Value = Convert.ToSingle(value);
                _suppress = false;
                _debounce.Stop();
                _debounce.Start();
            },
            "anny.parameter");
        _ = _commands.ExecuteAsync(command);
    }

    private async Task GeneratePreviewAsync()
    {
        if (_busy)
            return;
        _busy = true;
        try
        {
            StatusLabel.Text = "Erzeuge Anny-Vorschau…";
            var dest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Generated", "anny-live.glb");
            var glb = await _anny.GenerateGlbAsync(dest, AnnyHumanService.FromState(State));
            _loadPreview(glb);
            StatusLabel.Text = "Anny-Vorschau aktualisiert.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    public static ParametricHumanState Clone(ParametricHumanState source) =>
        new()
        {
            BackendId = source.BackendId,
            BackendVersion = source.BackendVersion,
            TopologyProfile = source.TopologyProfile,
            RigProfile = source.RigProfile,
            PhenotypeParameters = new Dictionary<string, float>(source.PhenotypeParameters),
            LocalShapeParameters = new Dictionary<string, float>(source.LocalShapeParameters),
            FacialActionParameters = new Dictionary<string, float>(source.FacialActionParameters),
            SourcePreset = source.SourcePreset,
            Seed = source.Seed
        };

    private static bool TryGet(ParametricHumanState state, string id, out float value)
    {
        var (map, key) = Map(state, id);
        return map.TryGetValue(key, out value);
    }

    private static void SetValue(ParametricHumanState state, string id, float value)
    {
        var (map, key) = Map(state, id);
        map[key] = value;
    }

    private static (Dictionary<string, float> map, string key) Map(ParametricHumanState state, string id)
    {
        var parts = id.Split(':', 2);
        var category = parts[0];
        var key = parts.Length > 1 ? parts[1] : parts[0];
        return category switch
        {
            "local" => (state.LocalShapeParameters, key),
            "face" => (state.FacialActionParameters, key),
            _ => (state.PhenotypeParameters, key)
        };
    }
}
