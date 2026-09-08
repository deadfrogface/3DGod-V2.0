namespace ThreeDGod.Core.Domain;

public sealed class LibraryAsset : DomainDocument
{
    public Guid LibraryAssetId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Prop";
    public string GlbPath { get; set; } = "";
    public string? PreviewPngPath { get; set; }
    public GeneratedAssetMetadata Provenance { get; set; } = new();
}
