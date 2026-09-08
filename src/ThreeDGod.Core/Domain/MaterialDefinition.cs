namespace ThreeDGod.Core.Domain;

public sealed class MaterialDefinition : DomainDocument
{
    public Guid MaterialId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public ColorRgba BaseColorFactor { get; set; } = new() { R = 1, G = 1, B = 1, A = 1 };
    public string? BaseColorTexture { get; set; }
    public float MetallicFactor { get; set; }
    public string? MetallicTexture { get; set; }
    public float RoughnessFactor { get; set; } = 1;
    public string? RoughnessTexture { get; set; }
    public string? NormalTexture { get; set; }
    public string? OcclusionTexture { get; set; }
    public ColorRgba EmissiveFactor { get; set; } = new();
    public string? EmissiveTexture { get; set; }
    public string AlphaMode { get; set; } = "OPAQUE";
    public float AlphaCutoff { get; set; } = 0.5f;
    public bool DoubleSided { get; set; }
    public TextureTransform? TextureTransform { get; set; }
    public string? Provenance { get; set; }
}
