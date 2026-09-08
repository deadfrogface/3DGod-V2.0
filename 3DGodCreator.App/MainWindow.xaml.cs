using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using HelixToolkit.Wpf;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Export;
using ThreeDGod.Workers;
using ThreeDGodCreator.App.Panels;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly IBlenderOperations _blenderService;
    private readonly PresetService _presetService;
    private readonly CharacterSystem _characterSystem;
    private readonly string _basePath;
    private string _currentPreviewPath = "";
    private DebugConsole _debugConsole = null!;

    /// <summary>
    /// Scale for height slider (Größe). Model size, not position.
    /// </summary>
    private ScaleTransform3D? _sculptScaleTransform;

    private readonly IFeatureAvailabilityService _features;
    private readonly CommandStack _commandStack;
    private readonly IDiagnosticService _diagnostics;
    private readonly IProjectService _projects;
    private readonly AnnyHumanService _anny;

    public MainWindow(
        ConfigService configService,
        IBlenderOperations blenderService,
        PresetService presetService,
        CharacterSystem characterSystem,
        IFeatureAvailabilityService features,
        CommandStack commandStack,
        IDiagnosticService diagnostics,
        IProjectService projects,
        AnnyHumanService anny)
    {
        InitializeComponent();
        _basePath = AppDomain.CurrentDomain.BaseDirectory;

        _configService = configService;
        _blenderService = blenderService;
        _presetService = presetService;
        _characterSystem = characterSystem;
        _features = features;
        _commandStack = commandStack;
        _diagnostics = diagnostics;
        _projects = projects;
        _anny = anny;

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
        _blenderService.OnLog += msg => DebugLog.Write($"[LegacyRuntime] {msg}");
        _blenderService.OnBlenderNotFound += () => Dispatcher.Invoke(() =>
        {
            DebugLog.Write("[LegacyRuntime] Unavailable – capability not installed. App bleibt stabil.");
        });
        _blenderService.OnBlenderFailed += (info) => Dispatcher.Invoke(() =>
        {
            DebugLog.Write($"[LegacyRuntime] {info.Code}: {info.Message}");
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
        ClothingPanel.Content = new ClothingPanel(_characterSystem, _features);
        PhysicsPanel.Content = new PhysicsPanel(_characterSystem, _features);
        MaterialPanel.Content = new MaterialEditorPanel(_characterSystem);
        PresetPanel.Content = new PresetBrowserPanel(_characterSystem);
        RiggingPanel.Content = new RiggingPanel(_characterSystem, _features);
        ExportPanel.Content = new ExportPanel(_characterSystem, _features, () => _currentPreviewPath);
        SettingsPanel.Content = new SettingsPanel(_characterSystem, _configService, _blenderService, this, _features);
        AiPanel.Content = new AiPanel(_characterSystem, _features, _anny, LoadPreview);
        ProblemsPanel.Content = new ProblemsPanel(_diagnostics);
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
                    _characterSystem.IsCurrentModelRigged = validation.HasRig && validation.HasSkin;

                    if (!validation.IsValid)
                    {
                        AppLogger.Write($"[Viewport] Model validation FAILED: {validation.Message}", isError: true);
                        DebugLog.Write($"[Viewport] Model validation: {validation.Message}");
                        MessageBox.Show($"Modell ungültig: {validation.Message}\n\nPfad: {path}",
                            "Modellfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (!_characterSystem.IsCurrentModelRigged)
                    {
                        AppLogger.Write($"[Viewport] Model has no armature/skin. Deformation sliders cannot work.", isError: true);
                        DebugLog.Write("[Viewport] Model has no armature/skin. Slider-Deformationen funktionieren nicht.");
                        Dispatcher.BeginInvoke(() =>
                        {
                            var r = MessageBox.Show(
                                "Das geladene Modell hat KEIN Skelett/Armature.\n\n" +
                                "Slider-Deformationen funktionieren nur mit rigged Modellen.\n\n" +
                                "Bitte verwende ein rigged Base-Modell oder deaktiviere die Deformations-Slider.",
                                "Modell nicht rigged",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        });
                    }

                    var vp = CreateViewport3D(content);
                    ViewportHost.Child = vp;
                    ApplySculptTransform(_characterSystem.SculptData);
                    if (FormPanel.Content is FormPanel fp)
                        fp.RefreshModelState();
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

    /// <summary>
    /// Creates viewport. Model centered at origin. Height slider controls scale (size).
    /// Viewport: Rechtsklick = Drehen, Shift+Rechtsklick = Verschieben, Mausrad = Zoom.
    /// </summary>
    private HelixViewport3D CreateViewport3D(Model3D content)
    {
        var centerOffset = GetModelCenterOffset(content);
        var centerTransform = new TranslateTransform3D(-centerOffset.X, -centerOffset.Y, -centerOffset.Z);
        _sculptScaleTransform = new ScaleTransform3D(1, 1, 1);

        var transformGroup = new Transform3DGroup();
        transformGroup.Children.Add(_sculptScaleTransform);
        transformGroup.Children.Add(centerTransform);
        var wrapper = new Model3DGroup { Transform = transformGroup };
        wrapper.Children.Add(content);

        var vp = new HelixViewport3D { Background = Brushes.Black };
        vp.RotateGesture = new System.Windows.Input.MouseGesture(System.Windows.Input.MouseAction.RightClick);
        vp.PanGesture = new System.Windows.Input.MouseGesture(System.Windows.Input.MouseAction.RightClick, System.Windows.Input.ModifierKeys.Shift);
        vp.PanGesture2 = new System.Windows.Input.MouseGesture(System.Windows.Input.MouseAction.None);
        vp.Children.Add(new DefaultLights());
        vp.Children.Add(new ModelVisual3D { Content = wrapper });
        vp.ZoomExtents();
        return vp;
    }

    /// <summary>
    /// Get model bounding box center so we can center at origin (decouple from feet/ground).
    /// </summary>
    private static Point3D GetModelCenterOffset(Model3D model)
    {
        var bounds = GetBounds(model, Matrix3D.Identity);
        return new Point3D(
            (bounds.X + bounds.SizeX) / 2,
            (bounds.Y + bounds.SizeY) / 2,
            (bounds.Z + bounds.SizeZ) / 2);
    }

    private static Rect3D GetBounds(Model3D model, Matrix3D parentMatrix)
    {
        var localMatrix = model.Transform?.Value ?? Matrix3D.Identity;
        var worldMatrix = Matrix3D.Multiply(parentMatrix, localMatrix);

        if (model is GeometryModel3D gm && gm.Geometry is MeshGeometry3D mesh)
            return new MatrixTransform3D(worldMatrix).TransformBounds(mesh.Bounds);

        if (model is Model3DGroup grp)
        {
            var union = Rect3D.Empty;
            foreach (Model3D child in grp.Children)
                union.Union(GetBounds(child, worldMatrix));
            return union;
        }
        return Rect3D.Empty;
    }

    /// <summary>
    /// Slider "Größe" (height) = Model scale (size). Slider "breast_size", "hip_width" = Blender only.
    /// </summary>
    public void ApplySculptTransform(Dictionary<string, int> sculptData)
    {
        if (_sculptScaleTransform == null) return;

        var height = sculptData.GetValueOrDefault("height", 50);

        // Scale: 50 = 1.0, 0 = 0.6, 100 = 1.4 (uniform)
        var s = 0.6 + (height / 100.0) * 0.8;
        var prevS = _sculptScaleTransform.ScaleX;

        _sculptScaleTransform.ScaleX = s;
        _sculptScaleTransform.ScaleY = s;
        _sculptScaleTransform.ScaleZ = s;

        if (Math.Abs(s - prevS) > 1e-6)
            AppLogger.Write($"[Transform] Scale changed from {prevS:F3} to {s:F3} (height={height})");
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
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            System.Windows.Input.ApplicationCommands.Undo,
            async (_, _) => await _commandStack.UndoAsync(),
            (_, e) => e.CanExecute = _commandStack.CanUndo));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            System.Windows.Input.ApplicationCommands.Redo,
            async (_, _) => await _commandStack.RedoAsync(),
            (_, e) => e.CanExecute = _commandStack.CanRedo));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            System.Windows.Input.ApplicationCommands.Undo, System.Windows.Input.Key.Z, System.Windows.Input.ModifierKeys.Control));
        InputBindings.Add(new System.Windows.Input.KeyBinding(
            System.Windows.Input.ApplicationCommands.Redo, System.Windows.Input.Key.Y, System.Windows.Input.ModifierKeys.Control));
    }

    private async void MenuNewProject_Click(object sender, RoutedEventArgs e)
    {
        var bundle = new ProjectBundle { Project = new ProjectDocument { Name = "Untitled" } };
        DebugLog.Write("[Project] Neues leeres Domain-Projekt im Speicher.");
        await Task.CompletedTask;
        _ = bundle;
    }

    private async void MenuOpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "3D God Projekt|*.3dgod", Title = "Projekt öffnen" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var bundle = await _projects.LoadAsync(dlg.FileName);
            DebugLog.Write($"[Project] Geladen: {bundle.Project.Name} ({bundle.Project.ProjectId})");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Projekt öffnen", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void MenuSaveProject_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "3D God Projekt|*.3dgod", Title = "Projekt speichern", FileName = "project.3dgod" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var bundle = new ProjectBundle
            {
                Project = new ProjectDocument { Name = Path.GetFileNameWithoutExtension(dlg.FileName) }
            };
            await _projects.SaveAsync(bundle, dlg.FileName);
            DebugLog.Write($"[Project] Gespeichert: {dlg.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Projekt speichern", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void MenuAnnyGenerate_Click(object sender, RoutedEventArgs e)
    {
        var probe = _anny.Probe();
        if (!_features.IsInvocable(FeatureIds.AnnyHuman))
        {
            MessageBox.Show(probe.Message, "Anny", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var dest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "Generated", $"anny-{DateTime.UtcNow:yyyyMMddHHmmss}.glb");
            DebugLog.Write("[Anny] Erzeuge Human…");
            var glb = await _anny.GenerateGlbAsync(dest);
            LoadPreview(glb);
            DebugLog.Write($"[Anny] GLB geladen: {glb}");
        }
        catch (Exception ex)
        {
            DebugLog.Write($"[Anny] {ex.Message}");
            MessageBox.Show(ex.Message, "Anny", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MenuExportGlb_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentPreviewPath) || !File.Exists(_currentPreviewPath))
        {
            MessageBox.Show("Kein verifiziertes Viewport-GLB zum Export.", "Export GLB", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new SaveFileDialog { Filter = "GLB|*.glb", FileName = "character.glb" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            GlbExportService.Export(_currentPreviewPath, dlg.FileName);
            DebugLog.Write($"[Export] GLB geschrieben: {dlg.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export GLB", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
