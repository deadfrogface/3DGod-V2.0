namespace ThreeDGodCreator.Core.Models;

public enum CharacterBaseKind
{
    Human,
    HumanoidCreature,
    Freeform
}

public enum CharacterPartKind
{
    BodyPart,
    Hair,
    FacialHair,
    Clothing,
    Accessory,
    CreaturePart,
    Weapon
}

public sealed class CharacterCreatorDefinition
{
    public CharacterBaseKind BaseKind { get; set; } = CharacterBaseKind.Human;
    public string BaseProvider { get; set; } = "anny";
    public string? Species { get; set; }

    /// <summary>Normalized morph values. Convention: -1..1 unless a provider documents a narrower range.</summary>
    public Dictionary<string, double> Morphs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, CharacterPartDefinition> Parts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> Appearance { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public CharacterCreatorDefinition Clone() => new()
    {
        BaseKind = BaseKind,
        BaseProvider = BaseProvider,
        Species = Species,
        Morphs = new Dictionary<string, double>(Morphs, StringComparer.OrdinalIgnoreCase),
        Parts = Parts.ToDictionary(kv => kv.Key, kv => kv.Value.Clone(), StringComparer.OrdinalIgnoreCase),
        Appearance = new Dictionary<string, string>(Appearance, StringComparer.OrdinalIgnoreCase)
    };
}

public sealed class CharacterPartDefinition
{
    public required string Id { get; init; }
    public CharacterPartKind Kind { get; init; }
    public string? AssetId { get; set; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public CharacterPartDefinition Clone() => new()
    {
        Id = Id,
        Kind = Kind,
        AssetId = AssetId,
        Enabled = Enabled,
        Parameters = new Dictionary<string, double>(Parameters, StringComparer.OrdinalIgnoreCase)
    };
}
