using System.IO;
using System.Numerics;
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
using ThreeDGod.Infrastructure;
using ThreeDGod.Infrastructure.Components;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using ThreeDGod.Rendering;
using ThreeDGod.Workers;
using ThreeDGodCreator.App.Localization;
using ThreeDGodCreator.App.Panels;
using ThreeDGodCreator.App.Windows;
using ThreeDGodCreator.Core;
using ThreeDGodCreator.Core.Localization;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

public partial class MainWindow : Window, ILocalizableView
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
    private readonly ActiveProjectSession _projectSession;
    private readonly AutosaveService _autosave;
    private readonly AnnyHumanService _anny;
    private readonly IAssetGenerationService _assets;
    private readonly IGarmentFitService _garmentFit;
    private readonly IImageTo3DService _imageTo3D;
    private readonly IReferenceImageGenerationService _referenceImages;
    private readonly IAutoRigService _autoRig;
    private readonly AllowlistedAiEditExecutor _aiEdits;
    private readonly ViewportSelectionService _viewportSelection;
    private readonly IFbxExportService _fbxExport;
    private readonly HelixViewportSession _viewportSession;
    private readonly IComponentManager _components;
    private readonly IWorkerUvComponentInstaller? _uvInstaller;
    private AnnyInspectorPanel? _annyInspector;
    private ProblemsPanel? _problemsPanel;

    public MainWindow(
        ConfigService configService,
        IBlenderOperations blenderService,
        PresetService presetService,
        CharacterSystem characterSystem,
        IFeatureAvailabilityService features,
        CommandStack commandStack,
        IDiagnosticService diagnostics,
        IProjectService projects,
        ActiveProjectSession projectSession,
        AutosaveService autosave,
        AnnyHumanService anny,
        IAssetGenerationService assets,
        IGarmentFitService garmentFit,
        IImageTo3DService imageTo3D,
        IReferenceImageGenerationService referenceImages,
        IAutoRigService autoRig,
        AllowlistedAiEditExecutor aiEdits,
        ViewportSelectionService viewportSelection,
        IFbxExportService fbxExport,
        IComponentManager components,
        IWorkerUvComponentInstaller? uvInstaller = null)
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
        _projectSession = projectSession;
        _autosave = autosave;
        _anny = anny;
        _assets = assets;
        _garmentFit = garmentFit;
        _imageTo3D = imageTo3D;
        _referenceImages = referenceImages;
        _autoRig = autoRig;
        _aiEdits = aiEdits;
        _viewportSelection = viewportSelection;
        _fbxExport = fbxExport;
        _components = components;
        _uvInstaller = uvInstaller;
        _viewportSession = new HelixViewportSession(_viewportSelection);
        _viewportSession.BindSelectionChanged(UpdateSelectionInspector);

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

        var cfg = _configService.Load();
        Loc.SetCulture(cfg.Language);
        Loc.CultureChanged += OnCultureChanged;
        ApplyTheme(cfg.Theme);
        ApplyLocalization();

        // Authoritative product path is ProjectBundle/Anny — do not auto-load legacy Form base as "the character".
        _projectSession.NewProject("Untitled");
        DebugLog.Write($"App gestartet. Basis: {_basePath}. Active project session ready.");
    }

    private void LoadPanels()
    {
        DebugConsoleHost.Content = new DebugConsole();
        _annyInspector = new AnnyInspectorPanel(_anny, _features, _commandStack, OnAnnyPreviewReady, _projectSession);
        AnnyPanel.Content = _annyInspector;
        FormPanel.Content = new FormPanel(_characterSystem, _projectSession);
        SculptPanel.Content = new SculptPanel(_characterSystem);
        NsfwPanel.Content = new NsfwPanel(_characterSystem);
        ClothingPanel.Content = new ClothingPanel(_characterSystem, _features, _garmentFit, _projectSession, RefreshViewportFromProject);
        PhysicsPanel.Content = new PhysicsPanel(_characterSystem, _features);
        MaterialPanel.Content = new MaterialEditorPanel(_characterSystem, _projectSession);
        PresetPanel.Content = new PresetBrowserPanel(_characterSystem);
        RiggingPanel.Content = new RiggingPanel(_characterSystem, _features, _autoRig, _projectSession, LoadPreviewOrRefreshProjectScene);
        ExportPanel.Content = new ExportPanel(_characterSystem, _features, _fbxExport, GetExportSourceGlb, _projectSession);
        SettingsPanel.Content = new SettingsPanel(_characterSystem, _configService, _blenderService, this, _features, _components, _uvInstaller);
        AiPanel.Content = new AiPanel(_characterSystem, _features, _anny, LoadPreviewOrRefreshProjectScene, _assets, _imageTo3D, _referenceImages, _projectSession, _aiEdits, _commandStack);
        _problemsPanel = new ProblemsPanel(_diagnostics, ShowDiagnosticIssueInViewport, ClearDiagnosticHighlight);
        ProblemsPanel.Content = _problemsPanel;
    }

    private void OnAnnyPreviewReady(string glbPath)
    {
        try
        {
            if (_annyInspector is not null)
                _projectSession.SetAnnyState(_annyInspector.State, markDirty: false);
            _projectSession.SetActiveMeshFromGlbFile(glbPath, "anny-body");
            _autosave.MarkDirty(_projectSession.Snapshot());
            _ = _autosave.ScheduleAutosaveAsync(_projectSession.Snapshot());
        }
        catch (Exception ex)
        {
            DebugLog.Write($"[Project] Mesh embed skipped: {ex.Message}");
        }
        RefreshViewportFromProject();
    }

    /// <summary>
    /// GLB paths refresh the composed project scene (body + garments).
    /// Non-mesh previews (e.g. reference PNG) keep single LoadPreview.
    /// </summary>
    private void LoadPreviewOrRefreshProjectScene(string path)
    {
        if (!string.IsNullOrWhiteSpace(path)
            && (path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase)))
        {
            RefreshViewportFromProject();
            return;
        }

        LoadPreview(path);
    }

    /// <summary>
    /// Authoritative viewport composition from ActiveProjectSession:
    /// body + fitted garments (and later attachments) as one Model3DGroup.
    /// </summary>
    public void RefreshViewportFromProject()
    {
        try
        {
            var work = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod", "ViewportScene");
            var parts = _projectSession.MaterializeSceneGlbs(work);
            if (parts.Count == 0)
            {
                ShowPlaceholder();
                return;
            }

            var root = new Model3DGroup();
            var allPositions = new List<Vector3>();
            var allIndices = new List<int>();
            string? primaryPath = null;
            var hasBones = false;

            foreach (var part in parts)
            {
                var content = GlbLoader.Load(part.GlbPath);
                if (content is null)
                    continue;
                root.Children.Add(content);
                primaryPath ??= part.GlbPath;
                try
                {
                    var (pos, idx) = MeshCompare.ReadMesh(part.GlbPath);
                    var baseIndex = allPositions.Count;
                    allPositions.AddRange(pos);
                    allIndices.AddRange(idx.Select(i => baseIndex + i));
                }
                catch (Exception meshEx)
                {
                    DebugLog.Write($"[Viewport] Scene part mesh read skipped ({part.Name}): {meshEx.Message}");
                }

                if (string.Equals(part.Role, "body", StringComparison.OrdinalIgnoreCase))
                {
                    var validation = ModelValidator.Validate(part.GlbPath);
                    hasBones = validation.HasRig && validation.HasSkin;
                    _characterSystem.IsCurrentModelRigged = hasBones;
                }
            }

            if (root.Children.Count == 0)
            {
                ShowPlaceholder();
                return;
            }

            _currentPreviewPath = primaryPath ?? "";
            _characterSystem.PreviewGlbPath = primaryPath;
            var vp = CreateViewport3D(root, allPositions, allIndices, hasRealBones: hasBones);
            ViewportHost.Child = vp;
            ApplySculptTransform(_characterSystem.SculptData);
            if (FormPanel.Content is FormPanel fp)
                fp.RefreshModelState();
            DebugLog.Write($"[Viewport] Composed scene: {parts.Count} part(s) from ActiveProjectSession.");
        }
        catch (Exception ex)
        {
            DebugLog.Write($"[Viewport] RefreshViewportFromProject failed: {ex.Message}");
            ShowAnatomyPreview();
        }
    }

    private string GetExportSourceGlb()
    {
        var work = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod", "ExportWork");
        try
        {
            var dest = Path.Combine(work, "composed-export.glb");
            var parts = _projectSession.MaterializeSceneGlbs(Path.Combine(work, "parts"));
            if (parts.Count == 0)
                return !string.IsNullOrWhiteSpace(_currentPreviewPath) && File.Exists(_currentPreviewPath)
                    ? _currentPreviewPath
                    : "";
            if (parts.Count == 1)
                return parts[0].GlbPath;
            return GlbExportService.ComposeScenes(
                parts.Select(p => (p.Name, p.GlbPath)).ToList(),
                dest);
        }
        catch (Exception ex)
        {
            DebugLog.Write($"[Export] Compose failed, falling back: {ex.Message}");
            return _projectSession.GetActiveMeshGlbPathOrMaterialize(work) ?? _currentPreviewPath ?? "";
        }
    }

    private void UpdateSelectionInspector(Guid? domainObjectId)
    {
        Dispatcher.Invoke(() =>
        {
            SelectionInfo.Text = domainObjectId is Guid id
                ? Loc.Get("preview.selection.domain", id)
                : Loc.Get("preview.selection.none");
        });
    }

    public void ApplyLocalization()
    {
        Title = Loc.Get("app.title");
        MenuFile.Header = Loc.Get("menu.file");
        MenuFileNew.Header = Loc.Get("menu.file.new");
        MenuFileOpen.Header = Loc.Get("menu.file.open");
        MenuFileSave.Header = Loc.Get("menu.file.save");
        MenuFileAnny.Header = Loc.Get("menu.file.anny");
        MenuFileExportGlb.Header = Loc.Get("menu.file.export_glb");
        MenuEdit.Header = Loc.Get("menu.edit");
        MenuEditUndo.Header = Loc.Get("action.undo");
        MenuEditRedo.Header = Loc.Get("action.redo");
        TabAnny.Header = Loc.Get("tab.anny");
        TabForm.Header = Loc.Get("tab.form");
        TabSculpt.Header = Loc.Get("tab.sculpt");
        TabNsfw.Header = Loc.Get("tab.nsfw");
        TabClothing.Header = Loc.Get("tab.clothing");
        TabPhysics.Header = Loc.Get("tab.physics");
        TabMaterial.Header = Loc.Get("tab.material");
        TabPresets.Header = Loc.Get("tab.presets");
        TabRigging.Header = Loc.Get("tab.rigging");
        TabExport.Header = Loc.Get("tab.export");
        TabSettings.Header = Loc.Get("tab.settings");
        TabAi.Header = Loc.Get("tab.ai");
        TabProblems.Header = Loc.Get("tab.problems");
        PreviewTitle.Text = Loc.Get("preview.title");
        PreviewControls.Text = Loc.Get("preview.controls");
        UpdateSelectionInspector(_viewportSelection.SelectedDomainObjectId);

        if (ExportPanel.Content is ILocalizableView export)
            export.ApplyLocalization();
        if (SettingsPanel.Content is ILocalizableView settings)
            settings.ApplyLocalization();
        if (ProblemsPanel.Content is ILocalizableView problems)
            problems.ApplyLocalization();
    }

    private void OnCultureChanged()
    {
        Dispatcher.Invoke(ApplyLocalization);
    }

    public bool ShowDiagnosticIssueInViewport(DiagnosticIssue issue)
    {
        var ok = _viewportSession.ShowDiagnostic(issue);
        if (!ok)
        {
            var reason = _viewportSession.Highlight.UnavailableReason
                         ?? (issue.Scene is null
                             ? "Diagnostic hat keine Scene-/Mesh-Referenz."
                             : "Problemstelle konnte im aktuellen Viewport nicht aufgelöst werden.");
            DebugLog.Write($"[Viewport] Diagnostic focus unavailable: {reason}");
            MessageBox.Show(reason, "Betroffenes Objekt", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        UpdateSelectionInspector(_viewportSelection.SelectedDomainObjectId);
        Tabs.SelectedIndex = 0;
        return true;
    }

    public void ClearDiagnosticHighlight() => _viewportSession.ClearDiagnostic();

    private void ApplyTheme(string theme)
    {
        Background = theme switch
        {
            "light" => new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            "cyberpunk" => new SolidColorBrush(Color.FromRgb(20, 10, 40)),
            _ => new SolidColorBrush(Color.FromRgb(30, 30, 30))
        };
    }

    public void ApplyThemePublic(string theme) => ApplyTheme(theme);

    private void RefreshSliders()
    {
        if (FormPanel.Content is FormPanel fp)
            fp.RefreshSliders();
    }

    public void LoadPreview(string path)
    {
        _currentPreviewPath = path;
        _characterSystem.PreviewGlbPath = null;
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
                        var positions = Array.Empty<Vector3>();
                        var indices = Array.Empty<int>();
                        try
                        {
                            if (ext == ".obj")
                            {
                                var imported = ObjImporter.ImportObj(path);
                                positions = imported.Positions.ToArray();
                                indices = imported.Indices.ToArray();
                            }
                        }
                        catch { /* hit-test still works; diagnostic verts may be unavailable */ }
                        var vp = CreateViewport3D(rotated, positions, indices, hasRealBones: false);
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
                _characterSystem.PreviewGlbPath = path;
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

                    IReadOnlyList<Vector3> positions = [];
                    IReadOnlyList<int> indices = [];
                    try
                    {
                        (positions, indices) = MeshCompare.ReadMesh(path);
                    }
                    catch (Exception meshEx)
                    {
                        DebugLog.Write($"[Viewport] Mesh positions unavailable for diagnostics: {meshEx.Message}");
                    }

                    var vp = CreateViewport3D(
                        content,
                        positions,
                        indices,
                        hasRealBones: _characterSystem.IsCurrentModelRigged);
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
    /// Viewport: Rechtsklick = Drehen, Shift+Rechtsklick = Verschieben, Mausrad = Zoom, Linksklick = Selection.
    /// </summary>
    private HelixViewport3D CreateViewport3D(
        Model3D content,
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<int> indices,
        bool hasRealBones)
    {
        var centerOffset = GetModelCenterOffset(content);
        var centerTransform = new TranslateTransform3D(-centerOffset.X, -centerOffset.Y, -centerOffset.Z);
        _sculptScaleTransform = new ScaleTransform3D(1, 1, 1);

        var meshId = Guid.NewGuid();
        var vp = _viewportSession.Attach(
            content,
            meshId,
            positions,
            indices,
            hasRealBones,
            _sculptScaleTransform,
            centerTransform);
        UpdateSelectionInspector(null);
        SelectionInfo.Text = Loc.Get("preview.selection.mesh", meshId);
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
        // Human Creator height is Anny phenotype regeneration — never pretend uniform scale is anatomy.
        if (_projectSession.IsAnnyHumanActive)
        {
            if (_sculptScaleTransform != null)
            {
                _sculptScaleTransform.ScaleX = 1;
                _sculptScaleTransform.ScaleY = 1;
                _sculptScaleTransform.ScaleZ = 1;
            }
            return;
        }

        if (_sculptScaleTransform == null) return;

        var height = sculptData.GetValueOrDefault("height", 50);

        // Legacy Form path only: uniform scale is NOT anatomical height morph.
        var s = 0.6 + (height / 100.0) * 0.8;
        var prevS = _sculptScaleTransform.ScaleX;

        _sculptScaleTransform.ScaleX = s;
        _sculptScaleTransform.ScaleY = s;
        _sculptScaleTransform.ScaleZ = s;

        if (Math.Abs(s - prevS) > 1e-6)
            AppLogger.Write($"[Transform] Legacy uniform scale {prevS:F3} → {s:F3} (Form height={height}; not Anny morph)");
    }

    public void ApplyMaterialOverridesToViewport(Dictionary<string, MaterialData> materials)
    {
        if (!IsShowing3DModel() || ViewportHost.Child is not HelixViewport3D vp)
            return;
        if (vp.Children.Count == 0 || vp.Children[0] is not ModelVisual3D visual || visual.Content == null)
            return;

        var slot = materials.GetValueOrDefault(_characterSystem.ActiveMaterialSlot)
            ?? materials.GetValueOrDefault("skin")
            ?? materials.Values.FirstOrDefault()
            ?? new MaterialData();
        GlbLoader.ApplyMaterialOverride(visual.Content, slot);

        if (!string.IsNullOrWhiteSpace(_characterSystem.PreviewGlbPath)
            && File.Exists(_characterSystem.PreviewGlbPath))
        {
            try
            {
                var preset = PbrMaterials.FromHex(
                    _characterSystem.ActiveMaterialSlot,
                    slot.Color,
                    (float)slot.Metallic,
                    (float)slot.Roughness);
                PbrMaterials.ApplyToGlb(_characterSystem.PreviewGlbPath, preset);
            }
            catch (Exception ex)
            {
                DebugLog.Write($"[Material] GLB PBR write skipped: {ex.Message}");
            }
        }
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
            Text = Loc.Get("preview.placeholder"),
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

    private void MenuSetupAssistant_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SetupAssistantWindow(_components, _uvInstaller) { Owner = this };
        dlg.ShowDialog();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            TryOfferAutosaveRecovery();

            var anyReady = _components.ListManifests().Any(m =>
                _components.GetState(m.ComponentId).State == ComponentState.Ready);
            var skipFlag = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DGod",
                "setup-skipped.flag");
            if (!anyReady && !File.Exists(skipFlag))
            {
                var dlg = new SetupAssistantWindow(_components, _uvInstaller) { Owner = this };
                dlg.ShowDialog();
                if (dlg.Skipped)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(skipFlag)!);
                    File.WriteAllText(skipFlag, DateTime.UtcNow.ToString("O"));
                }
            }
        }
        catch
        {
            // Setup assistant must never block core startup.
        }

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

    private async void TryOfferAutosaveRecovery()
    {
        try
        {
            var recoveries = _autosave.ListRecoveries()
                .Where(r => !string.Equals(r.ProjectName, "(unreadable)", StringComparison.Ordinal))
                .Take(5)
                .ToList();
            if (recoveries.Count == 0)
                return;

            var latest = recoveries[0];
            var answer = MessageBox.Show(
                $"Autosave gefunden: {latest.ProjectName}\n{latest.SavedUtc:u}\n\nWiederherstellen?",
                "Crash Recovery",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return;

            var bundle = await _autosave.RestoreAsync(latest.SessionId);
            await ApplyLoadedBundleAsync(bundle, projectPath: null);
            DebugLog.Write($"[Autosave] Wiederhergestellt: {latest.SessionId}");
        }
        catch (Exception ex)
        {
            DebugLog.Write($"[Autosave] Recovery skipped: {ex.Message}");
        }
    }

    private async Task ApplyLoadedBundleAsync(ProjectBundle bundle, string? projectPath)
    {
        _projectSession.LoadFrom(bundle, projectPath);
        _autosave.AssociateMainFile(projectPath);
        var state = _projectSession.ActiveCharacter?.ParametricHumanState;
        if (state is not null && _annyInspector is not null)
            await _annyInspector.ApplyStateAsync(state, generate: false);

        var work = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod", "ProjectMeshes");
        var meshPath = _projectSession.TryMaterializeActiveMesh(work);
        if (!string.IsNullOrWhiteSpace(meshPath) && File.Exists(meshPath))
        {
            RefreshViewportFromProject();
        }
        else if (state is not null && _features.IsInvocable(FeatureIds.AnnyHuman) && _annyInspector is not null)
        {
            await _annyInspector.ApplyStateAsync(state, generate: true);
        }

        // Restore material display from domain if present
        var mat = _projectSession.Bundle.Materials.FirstOrDefault();
        if (mat is not null)
        {
            var hex = $"#{(int)(mat.BaseColorFactor.R * 255):X2}{(int)(mat.BaseColorFactor.G * 255):X2}{(int)(mat.BaseColorFactor.B * 255):X2}";
            _characterSystem.SetMaterialPbr(mat.Name, hex, mat.RoughnessFactor, mat.MetallicFactor);
        }
    }

    private async void MenuNewProject_Click(object sender, RoutedEventArgs e)
    {
        _projectSession.NewProject("Untitled");
        _autosave.AssociateMainFile(null);
        if (_annyInspector is not null)
            await _annyInspector.ApplyStateAsync(new ParametricHumanState { BackendId = "anny", TopologyProfile = "anny", RigProfile = "anny" }, generate: false);
        _currentPreviewPath = "";
        ShowPlaceholder();
        DebugLog.Write("[Project] Neues Domain-Projekt (ActiveProjectSession).");
    }

    private async void MenuOpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "3D God Projekt|*.3dgod", Title = "Projekt öffnen" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var bundle = await _projects.LoadAsync(dlg.FileName);
            await ApplyLoadedBundleAsync(bundle, dlg.FileName);
            DebugLog.Write($"[Project] Geladen: {_projectSession.Bundle.Project.Name} ({_projectSession.Bundle.Project.ProjectId}) meshes={_projectSession.Bundle.MeshBytes.Count}");
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
            if (_annyInspector is not null)
                _projectSession.SetAnnyState(AnnyInspectorPanel.Clone(_annyInspector.State), markDirty: false);

            if (!string.IsNullOrWhiteSpace(_currentPreviewPath) && File.Exists(_currentPreviewPath)
                && Path.GetExtension(_currentPreviewPath).Equals(".glb", StringComparison.OrdinalIgnoreCase))
            {
                try { _projectSession.SetActiveMeshFromGlbFile(_currentPreviewPath, "body"); }
                catch (Exception meshEx) { DebugLog.Write($"[Project] Mesh capture: {meshEx.Message}"); }
            }

            SyncLegacyMaterialsIntoSession();

            var bundle = _projectSession.Snapshot();
            bundle.Project.Name = Path.GetFileNameWithoutExtension(dlg.FileName);
            bundle.Project.AppVersionLastSaved = "2.0.0";
            if (bundle.Characters.Count > 0)
                bundle.Characters[0].Name = bundle.Project.Name;

            await _projects.SaveAsync(bundle, dlg.FileName);
            _projectSession.MarkClean(dlg.FileName);
            _autosave.AssociateMainFile(dlg.FileName);
            await _autosave.FlushAsync();
            DebugLog.Write($"[Project] Gespeichert: {dlg.FileName} (meshBytes={bundle.MeshBytes.Count}, materials={bundle.Materials.Count}, garments={bundle.GarmentInstances.Count})");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Projekt speichern", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SyncLegacyMaterialsIntoSession()
    {
        foreach (var kv in _characterSystem.Materials)
        {
            var mat = kv.Value;
            var (r, g, b) = HexToRgb01(mat.Color);
            _projectSession.UpsertMaterial(kv.Key, r, g, b, 1f, (float)mat.Metallic, (float)mat.Roughness);
        }
    }

    private static (float r, float g, float b) HexToRgb01(string hex)
    {
        hex = (hex ?? "#cccccc").Trim().TrimStart('#');
        if (hex.Length < 6) return (0.8f, 0.8f, 0.8f);
        try
        {
            var rr = Convert.ToInt32(hex[..2], 16) / 255f;
            var gg = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
            var bb = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
            return (rr, gg, bb);
        }
        catch
        {
            return (0.8f, 0.8f, 0.8f);
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
            OnAnnyPreviewReady(glb);
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
        var src = GetExportSourceGlb();
        if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
        {
            MessageBox.Show("Kein Projekt-/Viewport-GLB zum Export.", "Export GLB", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new SaveFileDialog { Filter = "GLB|*.glb", FileName = "character.glb" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            GlbExportService.Export(src, dlg.FileName);
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
        public void ApplyMaterialOverrides(Dictionary<string, MaterialData> materials) =>
            _win.Dispatcher.Invoke(() => _win.ApplyMaterialOverridesToViewport(materials));
    }
}
