using System.Numerics;

namespace ThreeDGod.Rendering;

public sealed class ViewportObjectHit
{
    public Guid DomainObjectId { get; init; }
    public string Kind { get; init; } = "mesh";
}

/// <summary>
/// Maps Helix/render IDs to domain object IDs and tracks viewport selection state.
/// </summary>
public sealed class ViewportSelectionService
{
    private readonly Dictionary<int, ViewportObjectHit> _renderIdToDomain = new();

    public Guid? SelectedDomainObjectId { get; private set; }
    public bool SkeletonOverlayEnabled { get; private set; }
    public Guid? IsolatedDomainObjectId { get; private set; }

    public event Action<Guid?>? SelectionChanged;

    public void ClearRegistrations() => _renderIdToDomain.Clear();

    public void Register(int renderId, ViewportObjectHit hit) => _renderIdToDomain[renderId] = hit;

    public bool TryGetHit(int renderId, out ViewportObjectHit hit) =>
        _renderIdToDomain.TryGetValue(renderId, out hit!);

    public Guid? SelectRenderId(int renderId)
    {
        if (_renderIdToDomain.TryGetValue(renderId, out var hit))
        {
            SetSelection(hit.DomainObjectId);
            return hit.DomainObjectId;
        }

        SetSelection(null);
        return null;
    }

    public void SelectDomainObject(Guid domainObjectId) => SetSelection(domainObjectId);

    public void ClearSelection() => SetSelection(null);

    public void EnableSkeletonOverlay(bool hasRealBones) =>
        SkeletonOverlayEnabled = hasRealBones;

    public void Isolate(Guid? domainObjectId) => IsolatedDomainObjectId = domainObjectId;

    public void ClearIsolate() => IsolatedDomainObjectId = null;

    private void SetSelection(Guid? id)
    {
        if (SelectedDomainObjectId == id)
            return;
        SelectedDomainObjectId = id;
        SelectionChanged?.Invoke(id);
    }
}

/// <summary>
/// Resolves diagnostic scene references against real mesh positions for viewport focus/highlight.
/// </summary>
public sealed class ViewportDiagnosticHighlight
{
    public Guid? CharacterId { get; private set; }
    public Guid? MeshAssetId { get; private set; }
    public Guid? GarmentId { get; private set; }
    public Guid? RigId { get; private set; }
    public string? BoneId { get; private set; }
    public IReadOnlyList<int> VertexIndices { get; private set; } = [];
    public IReadOnlyList<int> TriangleIndices { get; private set; } = [];
    public double[]? WorldPosition { get; private set; }

    public bool Active { get; private set; }
    public IReadOnlyList<Vector3> HighlightedPositions { get; private set; } = [];
    public Vector3? FocusPoint { get; private set; }
    public string? UnavailableReason { get; private set; }

    public void Show(
        DiagnosticSceneTarget target,
        IReadOnlyList<Vector3> meshPositions,
        IReadOnlyList<int>? meshIndices = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        CharacterId = target.CharacterId;
        MeshAssetId = target.MeshAssetId;
        GarmentId = target.GarmentId;
        RigId = target.RigId;
        BoneId = target.BoneId;
        VertexIndices = target.VertexIndices;
        TriangleIndices = target.TriangleIndices;
        WorldPosition = target.WorldPosition;

        var points = new List<Vector3>();
        foreach (var vi in target.VertexIndices)
        {
            if (vi < 0 || vi >= meshPositions.Count)
                continue;
            points.Add(meshPositions[vi]);
        }

        if (meshIndices is not null)
        {
            foreach (var ti in target.TriangleIndices)
            {
                var baseIdx = ti * 3;
                if (baseIdx < 0 || baseIdx + 2 >= meshIndices.Count)
                    continue;
                var a = meshIndices[baseIdx];
                var b = meshIndices[baseIdx + 1];
                var c = meshIndices[baseIdx + 2];
                if (a < 0 || b < 0 || c < 0 || a >= meshPositions.Count || b >= meshPositions.Count || c >= meshPositions.Count)
                    continue;
                points.Add((meshPositions[a] + meshPositions[b] + meshPositions[c]) / 3f);
            }
        }

        if (target.WorldPosition is { Length: >= 3 } wp)
            FocusPoint = new Vector3((float)wp[0], (float)wp[1], (float)wp[2]);
        else if (points.Count > 0)
        {
            var sum = Vector3.Zero;
            foreach (var p in points)
                sum += p;
            FocusPoint = sum / points.Count;
        }
        else
            FocusPoint = null;

        HighlightedPositions = points;
        if (points.Count == 0 && FocusPoint is null)
        {
            Active = false;
            UnavailableReason = "Diagnostic target has no resolvable vertices, triangles, or world position for the loaded mesh.";
            return;
        }

        UnavailableReason = null;
        Active = true;
    }

    public void Clear()
    {
        Active = false;
        HighlightedPositions = [];
        FocusPoint = null;
        UnavailableReason = null;
        CharacterId = null;
        MeshAssetId = null;
        GarmentId = null;
        RigId = null;
        BoneId = null;
        VertexIndices = [];
        TriangleIndices = [];
        WorldPosition = null;
    }
}

/// <summary>
/// Domain IDs and mesh region referenced by a diagnostic issue for viewport focus.
/// </summary>
public sealed class DiagnosticSceneTarget
{
    public Guid? CharacterId { get; init; }
    public Guid? MeshAssetId { get; init; }
    public Guid? GarmentId { get; init; }
    public Guid? RigId { get; init; }
    public string? BoneId { get; init; }
    public IReadOnlyList<int> VertexIndices { get; init; } = [];
    public IReadOnlyList<int> TriangleIndices { get; init; } = [];
    public double[]? WorldPosition { get; init; }
}
