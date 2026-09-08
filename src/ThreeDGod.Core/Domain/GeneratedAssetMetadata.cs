namespace ThreeDGod.Core.Domain;

public sealed class GeneratedAssetMetadata : DomainDocument
{
    public string BackendId { get; set; } = "";
    public string BackendVersion { get; set; } = "";
    public string ModelId { get; set; } = "";
    public string ModelVersion { get; set; } = "";
    public string ModelHash { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string? NegativePrompt { get; set; }
    public long? Seed { get; set; }
    public List<string> InputAssetHashes { get; set; } = [];
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    public string LicenseProfileId { get; set; } = "";
    public Dictionary<string, string> Parameters { get; set; } = [];
    public string WorkerVersion { get; set; } = "";
}
