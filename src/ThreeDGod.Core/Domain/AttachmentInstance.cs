namespace ThreeDGod.Core.Domain;

public enum AttachmentType
{
    Necklace,
    Earring,
    Piercing,
    Ring,
    Weapon,
    Horn,
    Tail,
    Prosthetic,
    Hair,
    Beard,
    Custom
}

public sealed class AttachmentInstance : DomainDocument
{
    public Guid AttachmentId { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }
    public Guid AssetId { get; set; }
    public AttachmentType AttachmentType { get; set; } = AttachmentType.Custom;
    public string? ParentBoneSemantic { get; set; }
    public Guid? ParentBodyPartSlot { get; set; }
    public SpatialTransform LocalTransform { get; set; } = new();
    public string? PhysicsProfile { get; set; }
    public string? CollisionProfile { get; set; }
    public Dictionary<string, Guid> MaterialOverrides { get; set; } = [];
    public bool IsSkinned { get; set; }
}
