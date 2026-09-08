namespace ThreeDGod.Core.Domain;

public sealed class ProjectBundle
{
    public ProjectDocument Project { get; set; } = new();
    public List<CharacterDocument> Characters { get; set; } = [];
    public List<MeshAsset> Meshes { get; set; } = [];
    public List<MaterialDefinition> Materials { get; set; } = [];
    public List<RigDefinition> Rigs { get; set; } = [];
    public List<GarmentDefinition> GarmentDefinitions { get; set; } = [];
    public List<GarmentInstance> GarmentInstances { get; set; } = [];
    public List<AttachmentInstance> Attachments { get; set; } = [];
}

public interface IProjectMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    Task<ProjectBundle> MigrateAsync(ProjectBundle source, CancellationToken cancellationToken = default);
}
