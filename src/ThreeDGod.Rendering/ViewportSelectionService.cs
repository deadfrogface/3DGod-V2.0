namespace ThreeDGod.Rendering;

public sealed class ViewportObjectHit
{
    public Guid DomainObjectId { get; init; }
    public string Kind { get; init; } = "mesh";
}

public sealed class ViewportSelectionService
{
    private readonly Dictionary<int, ViewportObjectHit> _renderIdToDomain = new();
    public Guid? SelectedDomainObjectId { get; private set; }
    public bool SkeletonOverlayEnabled { get; private set; }

    public void Register(int renderId, ViewportObjectHit hit) => _renderIdToDomain[renderId] = hit;

    public Guid? SelectRenderId(int renderId)
    {
        if (_renderIdToDomain.TryGetValue(renderId, out var hit))
        {
            SelectedDomainObjectId = hit.DomainObjectId;
            return hit.DomainObjectId;
        }
        SelectedDomainObjectId = null;
        return null;
    }

    public void EnableSkeletonOverlay(bool hasRealBones) =>
        SkeletonOverlayEnabled = hasRealBones;
}

public sealed class ViewportDiagnosticHighlight
{
    public Guid? CharacterId { get; init; }
    public Guid? MeshAssetId { get; init; }
    public Guid? GarmentId { get; init; }
    public Guid? RigId { get; init; }
    public string? BoneId { get; init; }
    public IReadOnlyList<int> VertexIndices { get; init; } = [];
    public IReadOnlyList<int> TriangleIndices { get; init; } = [];
    public double[]? WorldPosition { get; init; }
    public bool Active { get; private set; }

    public void Show() => Active = true;
    public void Clear() => Active = false;
}
