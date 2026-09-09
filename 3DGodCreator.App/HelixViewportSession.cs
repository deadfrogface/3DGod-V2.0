using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Rendering;

namespace ThreeDGodCreator.App;

/// <summary>
/// Binds Helix viewport hit-testing, domain selection, and diagnostic highlights.
/// </summary>
public sealed class HelixViewportSession
{
    private readonly ViewportSelectionService _selection;
    private readonly ViewportDiagnosticHighlight _highlight = new();
    private HelixViewport3D? _viewport;
    private ModelVisual3D? _meshVisual;
    private PointsVisual3D? _highlightPoints;
    private LinesVisual3D? _skeletonLines;
    private IReadOnlyList<Vector3> _positions = [];
    private IReadOnlyList<int> _indices = [];
    private Guid _meshDomainId = Guid.Empty;
    private Action<Guid?>? _onSelectionChanged;

    public HelixViewportSession(ViewportSelectionService selection) => _selection = selection;

    public ViewportDiagnosticHighlight Highlight => _highlight;
    public Guid CurrentMeshDomainId => _meshDomainId;

    public void BindSelectionChanged(Action<Guid?> onChanged) => _onSelectionChanged = onChanged;

    public HelixViewport3D Attach(
        Model3D content,
        Guid meshDomainId,
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<int> indices,
        bool hasRealBones,
        ScaleTransform3D? sculptScale,
        Transform3D? centerTransform)
    {
        Detach();
        _meshDomainId = meshDomainId;
        _positions = positions;
        _indices = indices;
        _selection.ClearRegistrations();
        _selection.EnableSkeletonOverlay(hasRealBones);
        _highlight.Clear();

        var transformGroup = new Transform3DGroup();
        if (sculptScale is not null)
            transformGroup.Children.Add(sculptScale);
        if (centerTransform is not null)
            transformGroup.Children.Add(centerTransform);
        var wrapper = new Model3DGroup { Transform = transformGroup };
        wrapper.Children.Add(content);

        _meshVisual = new ModelVisual3D { Content = wrapper };
        _selection.Register(_meshVisual.GetHashCode(), new ViewportObjectHit
        {
            DomainObjectId = meshDomainId,
            Kind = "mesh"
        });

        _viewport = new HelixViewport3D { Background = Brushes.Black };
        _viewport.RotateGesture = new MouseGesture(MouseAction.RightClick);
        _viewport.PanGesture = new MouseGesture(MouseAction.RightClick, ModifierKeys.Shift);
        _viewport.PanGesture2 = new MouseGesture(MouseAction.None);
        _viewport.Children.Add(new DefaultLights());
        _viewport.Children.Add(_meshVisual);

        if (hasRealBones && _selection.SkeletonOverlayEnabled)
            AddSkeletonOverlay(positions);

        _viewport.MouseLeftButtonDown += OnMouseLeftButtonDown;
        _viewport.ZoomExtents();
        return _viewport;
    }

    public void Detach()
    {
        if (_viewport is not null)
            _viewport.MouseLeftButtonDown -= OnMouseLeftButtonDown;
        ClearHighlightVisuals();
        if (_viewport is not null && _skeletonLines is not null)
            _viewport.Children.Remove(_skeletonLines);
        _viewport = null;
        _meshVisual = null;
        _skeletonLines = null;
        _positions = [];
        _indices = [];
    }

    public bool ShowDiagnostic(DiagnosticIssue issue)
    {
        if (issue.Scene is null)
        {
            _highlight.Clear();
            ClearHighlightVisuals();
            return false;
        }

        var target = new DiagnosticSceneTarget
        {
            CharacterId = issue.Scene.CharacterId,
            MeshAssetId = issue.Scene.MeshAssetId ?? _meshDomainId,
            GarmentId = issue.Scene.GarmentId,
            RigId = issue.Scene.RigId,
            BoneId = issue.Scene.BoneId,
            VertexIndices = issue.Scene.VertexIndices,
            TriangleIndices = issue.Scene.TriangleIndices,
            WorldPosition = issue.Scene.WorldPosition
        };

        _highlight.Show(target, _positions, _indices);
        if (!_highlight.Active)
        {
            ClearHighlightVisuals();
            return false;
        }

        _selection.SelectDomainObject(target.MeshAssetId ?? _meshDomainId);
        ApplyHighlightVisuals();
        Focus(_highlight.FocusPoint);
        _onSelectionChanged?.Invoke(_selection.SelectedDomainObjectId);
        return true;
    }

    public void ClearDiagnostic()
    {
        _highlight.Clear();
        ClearHighlightVisuals();
    }

    private void Focus(Vector3? focus)
    {
        if (focus is null || _viewport?.Camera is not ProjectionCamera cam)
            return;
        var point = new Point3D(focus.Value.X, focus.Value.Y, focus.Value.Z);
        CameraHelper.LookAt(cam, point, 0.45, 350);
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewport is null || e.ChangedButton != MouseButton.Left || _meshVisual is null)
            return;

        var hitParams = new PointHitTestParameters(e.GetPosition(_viewport));
        var hitMesh = false;
        VisualTreeHelper.HitTest(
            _viewport,
            _ => HitTestFilterBehavior.Continue,
            result =>
            {
                if (result is RayMeshGeometry3DHitTestResult)
                {
                    hitMesh = true;
                    return HitTestResultBehavior.Stop;
                }
                return HitTestResultBehavior.Continue;
            },
            hitParams);

        if (hitMesh)
            _selection.SelectRenderId(_meshVisual.GetHashCode());
        else
            _selection.ClearSelection();

        _onSelectionChanged?.Invoke(_selection.SelectedDomainObjectId);
    }

    private void ApplyHighlightVisuals()
    {
        ClearHighlightVisuals();
        if (_viewport is null || _highlight.HighlightedPositions.Count == 0)
            return;

        var points = new Point3DCollection();
        foreach (var p in _highlight.HighlightedPositions)
            points.Add(new Point3D(p.X, p.Y, p.Z));

        _highlightPoints = new PointsVisual3D
        {
            Color = Colors.OrangeRed,
            Size = 6,
            Points = points
        };
        _viewport.Children.Add(_highlightPoints);
    }

    private void ClearHighlightVisuals()
    {
        if (_viewport is not null && _highlightPoints is not null)
            _viewport.Children.Remove(_highlightPoints);
        _highlightPoints = null;
    }

    private void AddSkeletonOverlay(IReadOnlyList<Vector3> positions)
    {
        if (_viewport is null || positions.Count < 2)
            return;

        var min = positions[0];
        var max = positions[0];
        foreach (var p in positions)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        var mid = (min + max) * 0.5f;
        _skeletonLines = new LinesVisual3D
        {
            Color = Colors.LimeGreen,
            Thickness = 2,
            Points =
            [
                new Point3D(mid.X, min.Y, mid.Z),
                new Point3D(mid.X, max.Y, mid.Z),
                new Point3D(min.X, mid.Y, mid.Z),
                new Point3D(max.X, mid.Y, mid.Z)
            ]
        };
        _viewport.Children.Add(_skeletonLines);
    }
}
