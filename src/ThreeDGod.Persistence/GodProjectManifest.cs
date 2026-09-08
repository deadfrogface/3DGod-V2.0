namespace ThreeDGod.Persistence;

public sealed class GodProjectManifest
{
    public string Format { get; set; } = "3dgod";
    public int FormatVersion { get; set; } = 1;
    public Guid ProjectId { get; set; }
    public string MinimumAppVersion { get; set; } = "3.0.0";
    public string RootProjectPath { get; set; } = "project.json";
    public List<ManifestFileEntry> Files { get; set; } = [];
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool Zip64 { get; set; } = true;
}

public sealed class ManifestFileEntry
{
    public string Path { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long Size { get; set; }
}
