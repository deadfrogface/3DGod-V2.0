namespace ThreeDGod.Core.Domain;

public sealed class MeshAsset : DomainDocument
{
    public Guid MeshAssetId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string SourceFormat { get; set; } = "glb";
    public string CanonicalGlbPath { get; set; } = "";
    public List<MeshLod> Lods { get; set; } = [];
    public int VertexCount { get; set; }
    public int TriangleCount { get; set; }
    public AxisAlignedBounds Bounds { get; set; } = new();
    public int UvSetCount { get; set; }
    public List<Guid> MaterialSlotIds { get; set; } = [];
    public bool HasNormals { get; set; }
    public bool HasTangents { get; set; }
    public bool HasSkin { get; set; }
    public bool HasMorphTargets { get; set; }
    public string ValidationState { get; set; } = "unknown";
    public string SourceHash { get; set; } = "";
    public GeneratedAssetMetadata? GeneratedMetadata { get; set; }
}

public sealed class MeshLod
{
    public int Level { get; set; }
    public string Path { get; set; } = "";
    public int TriangleCount { get; set; }
    public double? ErrorMetric { get; set; }
    public string Generator { get; set; } = "";
    public string GeneratorVersion { get; set; } = "";
}
