namespace ThreeDGod.Core.Domain;

public enum CharacterKind
{
    ParametricHuman,
    HumanoidCreature,
    FreeformCreature
}

public enum SourceRepresentation
{
    AnnyParameters,
    ImportedMesh,
    GeneratedMesh,
    ModularCreature,
    LegacyV2
}

public sealed class CharacterDocument : DomainDocument
{
    public Guid CharacterId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public CharacterKind CharacterKind { get; set; } = CharacterKind.ParametricHuman;
    public SourceRepresentation SourceRepresentation { get; set; } = SourceRepresentation.LegacyV2;
    public SpatialTransform RootTransform { get; set; } = new();
    public MeshSet MeshSet { get; set; } = new();
    public MaterialSet MaterialSet { get; set; } = new();
    public Guid? RigId { get; set; }
    public MorphState MorphState { get; set; } = new();
    public ParametricHumanState? ParametricHumanState { get; set; }
    public CreatureState? CreatureState { get; set; }
    public List<Guid> GarmentInstanceIds { get; set; } = [];
    public List<Guid> AttachmentInstanceIds { get; set; } = [];
    public GeneratedAssetMetadata? GeneratedAssetMetadata { get; set; }
    public int EditRevision { get; set; }
    public List<string> Tags { get; set; } = [];
    public string Notes { get; set; } = "";
}

public sealed class MeshSet
{
    public List<Guid> MeshAssetIds { get; set; } = [];
}

public sealed class MaterialSet
{
    public List<Guid> MaterialIds { get; set; } = [];
}

public sealed class MorphState
{
    public Dictionary<string, float> Weights { get; set; } = [];
}
