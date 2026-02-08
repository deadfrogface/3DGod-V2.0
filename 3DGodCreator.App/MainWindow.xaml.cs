using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HelixToolkit.Wpf;
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
        ApplyTheme(_configService.Load().Theme);

        if (_presetService.Exists("default"))
            _characterSystem.LoadPreset("default");
        else
            _characterSystem.LoadBaseModel(_characterSystem.Config.Gender);

        _blenderService.OnLog += _ => { };
    }

    private void LoadPanels()
    {
        FormPanel.Content = new FormPanel(_characterSystem);
        SculptPanel.Content = new SculptPanel(_characterSystem);
        NsfwPanel.Content = new NsfwPanel(_characterSystem);
        ClothingPanel.Content = new ClothingPanel(_characterSystem);
        RiggingPanel.Content = new RiggingPanel(_characterSystem);
        ExportPanel.Content = new ExportPanel(_characterSystem);
        SettingsPanel.Content = new SettingsPanel(_characterSystem, _configService, this);
        AiPanel.Content = new AiPanel(_characterSystem);
    }

    private void ApplyTheme(string theme)
    {
        if (theme == "dark")
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
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
        {
            path = Path.GetFullPath(Path.Combine(_basePath, path));
        }

        if (!File.Exists(path)) return;

        try
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is ".obj" or ".glb" or ".3ds" or ".stl")
            {
                try
                {
                    var importer = new ModelImporter();
                    var content = importer.Load(path);
                    if (content != null)
                    {
                        var vp = new HelixViewport3D { Background = Brushes.Black };
                        vp.Children.Add(new DefaultLights());
                        vp.Children.Add(new ModelVisual3D { Content = content });
                        vp.ZoomExtents();
                        ViewportHost.Child = vp;
                    }
                    else
                        ShowPlaceholder();
                }
                catch
                {
                    ShowPlaceholder();
                }
            }
            else if (ext is ".png" or ".jpg" or ".jpeg")
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(Path.GetFullPath(path));
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                PreviewImage.Source = bi;
                ViewportHost.Child = PreviewImage;
            }
        }
        catch
        {
            ShowPlaceholder();
        }
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

    private void LogToDebug(string msg) { }

    private class ViewportAdapter : IViewport
    {
        private readonly MainWindow _win;

        public ViewportAdapter(MainWindow win) => _win = win;

        public void LoadPreview(string path) => _win.Dispatcher.Invoke(() => _win.LoadPreview(path));
        public void UpdateView() => _win.Dispatcher.Invoke(_win.UpdateView);
        public void UpdatePreview(Dictionary<string, bool> _, Dictionary<string, List<string>> __) => UpdateView();
    }
}
