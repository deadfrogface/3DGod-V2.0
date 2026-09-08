namespace ThreeDGod.Core.Domain;

public enum GarmentType
{
    Shirt,
    Jacket,
    Coat,
    Pants,
    Shorts,
    Skirt,
    Dress,
    Shoes,
    Gloves,
    Armor,
    Custom
}

public sealed class GarmentDefinition : DomainDocument
{
    public Guid GarmentDefinitionId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public GarmentType GarmentType { get; set; } = GarmentType.Custom;
    public string GeneratorBackend { get; set; } = "";
    public string? PatternDefinition { get; set; }
    public List<ParameterCatalogEntry> ParameterCatalog { get; set; } = [];
    public List<Guid> BaseMaterialIds { get; set; } = [];
    public List<string> CompatibilityTags { get; set; } = [];
    public string RigStrategy { get; set; } = "";
    public string? SimulationProfile { get; set; }
}

public sealed class GarmentInstance : DomainDocument
{
    public Guid GarmentInstanceId { get; set; } = Guid.NewGuid();
    public Guid DefinitionId { get; set; }
    public Guid CharacterId { get; set; }
    public Dictionary<string, float> ParameterValues { get; set; } = [];
    public Guid? MeshAssetId { get; set; }
    public Dictionary<string, Guid> MaterialOverrides { get; set; } = [];
    public string? SkinBinding { get; set; }
    public string FitState { get; set; } = "unfitted";
    public string CollisionState { get; set; } = "unknown";
}
