namespace ThreeDGod.Core.Domain;

public sealed class CreatureState : DomainDocument
{
    public string BaseFamily { get; set; } = "";
    public string SkeletonProfileId { get; set; } = "";
    public List<BodyPartSlot> BodyPartSlots { get; set; } = [];
    public List<BodyPartSlot> ExtraBodyParts { get; set; } = [];
    public Dictionary<string, float> CreatureMorphParameters { get; set; } = [];
    public string TopologyCompatibilityGroup { get; set; } = "";
    public string RigStrategy { get; set; } = "";
    public BodyPlan BodyPlan { get; set; } = new();
}

public sealed class BodyPlan
{
    public List<string> SemanticLimbDescriptors { get; set; } = [];
    public int TailCount { get; set; }
    public int WingCount { get; set; }
    public int ExtraLimbCount { get; set; }
    public bool IsBiped { get; set; }
    public bool IsQuadruped { get; set; }
    public List<string> CustomTags { get; set; } = [];
}

public enum SemanticBodyPartType
{
    Head,
    Torso,
    LeftHand,
    RightHand,
    LeftFoot,
    RightFoot,
    Tail,
    Horn,
    Ear,
    Tusk,
    Wing,
    Prosthetic,
    Custom
}

public sealed class BodyPartSlot
{
    public Guid SlotId { get; set; } = Guid.NewGuid();
    public SemanticBodyPartType SemanticType { get; set; } = SemanticBodyPartType.Custom;
    public Guid? MeshAssetId { get; set; }
    public string? ParentBoneSemantic { get; set; }
    public string? BoundaryDefinition { get; set; }
    public SpatialTransform LocalTransform { get; set; } = new();
    public Dictionary<string, Guid> MaterialOverrides { get; set; } = [];
    public string? RigBinding { get; set; }
    public GeneratedAssetMetadata? GenerationProvenance { get; set; }
}
