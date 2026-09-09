namespace ThreeDGod.Core.Domain;

public sealed class ReferenceSet : DomainDocument
{
    public Guid ReferenceSetId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<Guid> ImageIds { get; set; } = [];
}

public sealed class ReferenceImage : DomainDocument
{
    public Guid ReferenceImageId { get; set; } = Guid.NewGuid();
    public Guid ReferenceSetId { get; set; }
    public string Prompt { get; set; } = "";
    public long? Seed { get; set; }
    public string BackendId { get; set; } = "";
    public string ModelId { get; set; } = "";
    public string ModelHash { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public string Sha256 { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string LicenseProfileId { get; set; } = "";
    public string Source { get; set; } = "unspecified";
}
