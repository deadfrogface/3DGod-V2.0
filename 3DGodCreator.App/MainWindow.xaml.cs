using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using ThreeDGodCreator.App.Panels;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly BlenderService _blenderService;
    private readonly PresetService _presetService;
    private readonly CharacterSystem _characterSystem;
    private readonly string _basePath;
    private string _currentPreviewPath = "";
    private DebugConsole _debugConsole = null!;
    private TranslateTransform3D? _sculptTranslateTransform;

    public MainWindow()
    {
        InitializeComponent();
        _basePath = AppDomain.CurrentDomain.BaseDirectory;

        _configService = new ConfigService();
        _blenderService = new BlenderService(_configService);
        _presetService = new PresetService();
        _characterSystem = new CharacterSystem(_configService, _blenderService, _presetService);

        _characterSystem.Viewport = new ViewportAdapter(this);
        _characterSystem.SliderSyncCallback = RefreshSliders;

        LoadPanels();
        _debugConsole = (DebugConsole)DebugConsoleHost.Content;
        _debugConsole.OnOpenSettingsRequested = () => Tabs.SelectedIndex = 9;
        DebugLog.OnMessage += msg => Dispatcher.Invoke(() => _debugConsole?.Log(msg));

        // Run project readiness check - logs to error_log.txt and Debug console
        var readiness = ProjectReadinessService.RunFullCheck();
        foreach (var line in readiness.SummaryLines)
            DebugLog.Write($"[Startup] {line}");
        if (!readiness.AllCriticalPassed)
            DebugLog.Write($"[Startup] Einige Prüfungen fehlgeschlagen. Details: {StartupLogger.GetLogFilePath()}");
        _blenderService.OnLog += msg => DebugLog.Write($"[Blender] {msg}");
        _blenderService.OnBlenderNotFound += () => Dispatcher.Invoke(() =>
        {
            Tabs.SelectedIndex = 9;
            MessageBox.Show(
                "Blender wurde nicht gefunden.\n\nBitte setze den Blender-Pfad in den Einstellungen (z.B. C:\\Program Files\\Blender Foundation\\Blender 4.0\\blender.exe).",
                "Blender fehlt",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        });
        _blenderService.OnBlenderFailed += (info) => Dispatcher.Invoke(() =>
        {
            Tabs.SelectedIndex = 9;
            var msg = $"{info.Message}\n\n";
            if (!string.IsNullOrEmpty(info.Detail)) msg += $"Details: {info.Detail}\n\n";
            if (!string.IsNullOrEmpty(info.SuggestedFix)) msg += $"-> {info.SuggestedFix}\n\n";
            msg += $"Log: {AppLogger.GetLogFilePath()}";
            MessageBox.Show(msg, "Blender Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        });

        ApplyTheme(_configService.Load().Theme);

        if (_presetService.Exists("default"))
            _characterSystem.LoadPreset("default");
        else
            _characterSystem.LoadBaseModel(_characterSystem.Config.Gender);

        DebugLog.Write($"App gestartet. Basis: {_basePath}");
    }

    private void LoadPanels()
    {
        DebugConsoleHost.Content = new DebugConsole();
        FormPanel.Content = new FormPanel(_characterSystem);
        SculptPanel.Content = new SculptPanel(_characterSystem);
        NsfwPanel.Content = new NsfwPanel(_characterSystem);
        ClothingPanel.Content = new ClothingPanel(_characterSystem);
        PhysicsPanel.Content = new PhysicsPanel(_characterSystem);
        MaterialPanel.Content = new MaterialEditorPanel(_characterSystem);
        PresetPanel.Content = new PresetBrowserPanel(_characterSystem);
        RiggingPanel.Content = new RiggingPanel(_characterSystem);
        ExportPanel.Content = new ExportPanel(_characterSystem);
        SettingsPanel.Content = new SettingsPanel(_characterSystem, _configService, _blenderService, this);
        AiPanel.Content = new AiPanel(_characterSystem);
    }

    private void ApplyTheme(string theme)
    {
        Background = theme switch
        {
            "light" => new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            "cyberpunk" => new SolidColorBrush(Color.FromRgb(20, 10, 40)),
            _ => new SolidColorBrush(Color.FromRgb(30, 30, 30))
        };
    }

    private void RefreshSliders()
    {
        if (FormPanel.Content is FormPanel fp)
            fp.RefreshSliders();
    }

    public void LoadPreview(string path)
    {
        _currentPreviewPath = path;
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(_basePath, path));

        if (!File.Exists(path))
        {
            DebugLog.Write($"[Viewport] Pfad nicht gefunden: {path}");
            ShowAnatomyPreview();
            return;
        }

        try
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is ".obj" or ".3ds" or ".stl")
            {
                try
                {
                    var importer = new ModelImporter();
                    var content = importer.Load(path);
                    if (content != null)
                    {
                        var rotated = WrapWithUprightTransform(content);
                        var vp = CreateViewport3D(rotated);
                        ViewportHost.Child = vp;
                        DebugLog.Write($"[Viewport] 3D-Modell geladen: {path}");
                    }
                    else
                        ShowAnatomyPreview();
                }
                catch (Exception ex)
                {
                    DebugLog.Write($"[Viewport] 3D-Import fehlgeschlagen: {ex.Message}");
                    ShowAnatomyPreview();
                }
            }
            else if (ext == ".glb")
            {
                var content = GlbLoader.Load(path);
                if (content != null)
                {
                    var validation = ModelValidator.Validate(path);
                    if (!validation.IsValid)
                    {
                        DebugLog.Write($"[Viewport] Model validation: {validation.Message}");
                        AppLogger.Write($"[Viewport] Model validation failed: {validation.Message}", isError: true);
                    }
                    else if (!string.IsNullOrEmpty(validation.Message))
                    {
                        DebugLog.Write($"[Viewport] {validation.Message}");
                    }
                    var vp = CreateViewport3D(content);
                    ViewportHost.Child = vp;
                    ApplySculptTransform(_characterSystem.SculptData);
                    DebugLog.Write($"[Viewport] GLB-Modell geladen: {path}");
                }
                else
                {
                    DebugLog.Write("[Viewport] GLB-Import fehlgeschlagen – zeige Anatomie-Vorschau");
                    ShowAnatomyPreview();
                }
            }
            else if (ext is ".png" or ".jpg" or ".jpeg")
            {
                LoadPreviewImage(path);
            }
        }
        catch (Exception ex)
        {
            DebugLog.Write($"[Viewport] Fehler: {ex.Message}");
            ShowAnatomyPreview();
        }
    }

    private void LoadPreviewImage(string path)
    {
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(Path.GetFullPath(path));
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            PreviewImage.Source = bi;
            ViewportHost.Child = PreviewImage;
        }
        catch
        {
            ShowPlaceholder();
        }
    }

    public void UpdatePreviewFromAnatomy(Dictionary<string, bool> anatomy)
    {
        if (IsShowing3DModel())
            return;
        var imgPath = GetAnatomyPreviewPath(anatomy);
        if (!string.IsNullOrEmpty(imgPath) && File.Exists(imgPath))
            LoadPreviewImage(imgPath);
        else
            ShowPlaceholder();
    }

    private bool IsShowing3DModel()
    {
        if (string.IsNullOrEmpty(_currentPreviewPath)) return false;
        var ext = Path.GetExtension(_currentPreviewPath).ToLowerInvariant();
        return ext is ".glb" or ".obj" or ".3ds" or ".stl";
    }

    private static Model3DGroup WrapWithUprightTransform(Model3D content)
    {
        var group = new Model3DGroup();
        group.Children.Add(content);
        group.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180));
        return group;
    }

    private HelixViewport3D CreateViewport3D(Model3D content)
    {
        _sculptTranslateTransform = new TranslateTransform3D(0, 0, 0);
        var transformGroup = new Transform3DGroup();
        transformGroup.Children.Add(_sculptTranslateTransform);
        var wrapper = new Model3DGroup { Transform = transformGroup };
        wrapper.Children.Add(content);

        var vp = new HelixViewport3D { Background = Brushes.Black };
        vp.Children.Add(new DefaultLights());
        vp.Children.Add(new ModelVisual3D { Content = wrapper });
        vp.ZoomExtents();
        return vp;
    }

    /// <summary>
    /// Apply slider values as translation (move) in 3D space. Values 0-100 map to offsets.
    /// </summary>
    public void ApplySculptTransform(Dictionary<string, int> sculptData)
    {
        if (_sculptTranslateTransform == null) return;

        var height = sculptData.GetValueOrDefault("height", 50);
        var hipWidth = sculptData.GetValueOrDefault("hip_width", 50);
        var breastSize = sculptData.GetValueOrDefault("breast_size", 50);

        var scale = 0.02;
        var y = (height - 50) * scale;
        var x = (hipWidth - 50) * scale * 0.5;
        var z = (breastSize - 50) * scale * 0.3;

        _sculptTranslateTransform.OffsetX = x;
        _sculptTranslateTransform.OffsetY = y;
        _sculptTranslateTransform.OffsetZ = z;
    }

    private string GetAnatomyPreviewPath(Dictionary<string, bool> anatomy)
    {
        var previewDir = Path.Combine(_basePath, "assets", "view_preview");
        if (!Directory.Exists(previewDir)) return "";

        if (anatomy.GetValueOrDefault("organs", false))
            return Path.Combine(previewDir, "skin_fat_muscle_bone_organs.png");
        if (anatomy.GetValueOrDefault("bone", false))
            return Path.Combine(previewDir, "skin_fat_muscle.png");
        if (anatomy.GetValueOrDefault("muscle", false))
            return Path.Combine(previewDir, "skin_fat_muscle.png");
        if (anatomy.GetValueOrDefault("fat", true))
            return Path.Combine(previewDir, "skin_fat.png");
        return Path.Combine(previewDir, "skin.png");
    }

    private void ShowAnatomyPreview()
    {
        UpdatePreviewFromAnatomy(_characterSystem.AnatomyState);
    }

    private void ShowPlaceholder()
    {
        var grid = new System.Windows.Controls.Grid { Background = Brushes.Black };
        grid.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "3D-Vorschau\n(GLB/OBJ/PNG)",
            Foreground = Brushes.Gray,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 18
        });
        ViewportHost.Child = grid;
    }

    public void UpdateView()
    {
        if (!string.IsNullOrEmpty(_currentPreviewPath))
            LoadPreview(_currentPreviewPath);
    }

    private static readonly System.Windows.Input.RoutedCommand ToggleDebugCommand = new();

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            ToggleDebugCommand, System.Windows.Input.Key.F12, System.Windows.Input.ModifierKeys.None));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(ToggleDebugCommand, (_, _) =>
        {
            DebugPanelHost.Visibility = DebugPanelHost.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
        }));
    }

    private class ViewportAdapter : IViewport
    {
        private readonly MainWindow _win;

        public ViewportAdapter(MainWindow win) => _win = win;

        public void LoadPreview(string path) => _win.Dispatcher.Invoke(() => _win.LoadPreview(path));
        public void UpdateView() => _win.Dispatcher.Invoke(_win.UpdateView);
        public void UpdatePreview(Dictionary<string, bool> anatomy, Dictionary<string, List<string>> _) =>
            _win.Dispatcher.Invoke(() => _win.UpdatePreviewFromAnatomy(anatomy));
        public void ApplySculptTransform(Dictionary<string, int> sculptData) =>
            _win.Dispatcher.Invoke(() => _win.ApplySculptTransform(sculptData));
    }
}
