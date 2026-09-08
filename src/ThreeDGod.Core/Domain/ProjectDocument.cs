namespace ThreeDGod.Core.Domain;

public sealed class ProjectDocument : DomainDocument
{
    public Guid ProjectId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int FormatVersion { get; set; } = 1;
    public string AppVersionCreated { get; set; } = "";
    public string AppVersionLastSaved { get; set; } = "";
    public Guid? ActiveSceneId { get; set; }
    public List<Guid> SceneIds { get; set; } = [];
    public List<Guid> CharacterIds { get; set; } = [];
    public List<Guid> AssetIds { get; set; } = [];
    public List<ExportProfile> ExportProfiles { get; set; } = [];
    public ProjectSettings Settings { get; set; } = new();
    public string ProvenanceSummary { get; set; } = "";
}

public sealed class ExportProfile
{
    public Guid ExportProfileId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Target { get; set; } = "glb";
}

public sealed class ProjectSettings
{
    public string Locale { get; set; } = "de";
    public string Theme { get; set; } = "dark";
    public Dictionary<string, string> Values { get; set; } = [];
}
