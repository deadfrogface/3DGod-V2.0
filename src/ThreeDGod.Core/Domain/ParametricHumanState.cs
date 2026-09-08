namespace ThreeDGod.Core.Domain;

public sealed class ParametricHumanState : DomainDocument
{
    public string BackendId { get; set; } = "";
    public string BackendVersion { get; set; } = "";
    public string TopologyProfile { get; set; } = "";
    public string RigProfile { get; set; } = "";
    public Dictionary<string, float> PhenotypeParameters { get; set; } = [];
    public Dictionary<string, float> LocalShapeParameters { get; set; } = [];
    public Dictionary<string, float> FacialActionParameters { get; set; } = [];
    public Dictionary<string, float> PoseParameters { get; set; } = [];
    public long? Seed { get; set; }
    public string? SourcePreset { get; set; }
}

public sealed class ParameterCatalogEntry
{
    public string Key { get; set; } = "";
    public string DisplayNameResourceKey { get; set; } = "";
    public string Category { get; set; } = "";
    public float Min { get; set; }
    public float Max { get; set; } = 1;
    public float Default { get; set; }
    public string Unit { get; set; } = "";
    public string? SymmetryGroup { get; set; }
    public string BackendMapping { get; set; } = "";
    public bool IsAdvanced { get; set; }
}
