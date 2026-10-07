using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;
using ThreeDGod.Export;
using ThreeDGod.Persistence;

namespace ThreeDGod.Infrastructure;

/// <summary>
/// Headless product workflow over ActiveProjectSession + GodProjectArchive.
/// Stage 27 integration tests exercise these PRODUCT services (no soft stubs).
/// </summary>
public sealed class ProductWorkflowService
{
    private readonly ActiveProjectSession _session;
    private readonly IProjectService _projects;
    private readonly AutosaveService? _autosave;
    private readonly AllowlistedAiEditExecutor _edits;
    private readonly string _workRoot;
    private readonly ICreatureAssembly? _creatures;
    private readonly ICreatureTextEditService? _creatureEdits;
    private readonly IFreeformCharacterPipeline? _freeform;

    public ProductWorkflowService(
        ActiveProjectSession session,
        IProjectService projects,
        AllowlistedAiEditExecutor edits,
        AutosaveService? autosave = null,
        string? workRoot = null,
        ICreatureAssembly? creatures = null,
        ICreatureTextEditService? creatureEdits = null,
        IFreeformCharacterPipeline? freeform = null)
    {
        _session = session;
        _projects = projects;
        _edits = edits;
        _autosave = autosave;
        _workRoot = workRoot ?? Path.Combine(Path.GetTempPath(), "3dgod-product-workflow");
        _creatures = creatures;
        _creatureEdits = creatureEdits;
        _freeform = freeform;
        Directory.CreateDirectory(_workRoot);
    }

    public ActiveProjectSession Session => _session;

    public void NewHumanProject(string name = "Human")
    {
        _session.NewProject(name);
        _autosave?.AssociateMainFile(null);
        NotifyAutosave();
    }

    public void NewOrcProject(string name = "Orc")
    {
        CreateCreatureProject(name, static (builder, bundle, root) => builder.CreateOrc(bundle, root));
    }

    public void NewRatProject(string name = "Humanoid Rat")
    {
        CreateCreatureProject(name, static (builder, bundle, root) => builder.CreateRat(bundle, root));
    }

    private void CreateCreatureProject(
        string name,
        Func<ICreatureAssembly, ProjectBundle, string, CharacterDocument> factory)
    {
        if (_creatures is null)
            throw new InvalidOperationException("Creature assembly service is not configured.");

        var bundle = new ProjectBundle
        {
            Project = new ProjectDocument
            {
                Name = name,
                AppVersionCreated = "2.0.0",
                AppVersionLastSaved = "2.0.0"
            }
        };
        var root = Path.Combine(_workRoot, "creature-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var character = factory(_creatures, bundle, root);
        character.Name = name;
        _session.LoadCreatedCreature(bundle, character.CharacterId);
        _autosave?.AssociateMainFile(null);
        NotifyAutosave();
    }

    public async Task NewFreeformProjectAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (_freeform is null)
            throw new InvalidOperationException("Freeform character pipeline is not configured.");
        var bundle = new ProjectBundle
        {
            Project = new ProjectDocument
            {
                Name = string.IsNullOrWhiteSpace(prompt) ? "Freeform Creature" : prompt.Trim(),
                AppVersionCreated = "2.0.0",
                AppVersionLastSaved = "2.0.0"
            }
        };
        var root = Path.Combine(_workRoot, "freeform-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var character = await _freeform.RunAsync(prompt, bundle, root, cancellationToken).ConfigureAwait(false);
        _session.LoadCreatedCreature(bundle, character.CharacterId);
        _autosave?.AssociateMainFile(null);
        NotifyAutosave();
    }

    public async Task ApplyCreatureEditAsync(string prompt, CommandStack stack, CancellationToken cancellationToken = default)
    {
        if (_creatureEdits is null)
            throw new InvalidOperationException("Creature edit service is not configured.");
        var snapshot = _session.Snapshot();
        var character = snapshot.Characters.FirstOrDefault(x => x.CharacterId == _session.ActiveCharacter?.CharacterId)
            ?? throw new InvalidOperationException("No active creature.");
        if (character.CreatureState is null)
            throw new InvalidOperationException("Active character is not a creature.");
        var root = Path.Combine(_workRoot, "creature-edit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await _creatureEdits.ApplyAsync(prompt, snapshot, character, root, stack, cancellationToken).ConfigureAwait(false);
        _session.LoadCreatedCreature(snapshot, character.CharacterId, _session.ProjectPath);
        NotifyAutosave();
    }

    public void ApplyAnnyState(ParametricHumanState state)
    {
        _session.SetAnnyState(state);
        NotifyAutosave();
    }

    public void SetBodyMeshFromFile(string glbPath)
    {
        _session.SetActiveMeshFromGlbFile(glbPath, "body");
        NotifyAutosave();
    }

    public void SetBodyMeshBytes(byte[] glbBytes)
    {
        _session.SetActiveMeshBytes(glbBytes, "body", SourceRepresentation.AnnyParameters);
        NotifyAutosave();
    }

    public void UpsertMaterial(string slot, float r, float g, float b, float metallic, float roughness)
    {
        _session.UpsertMaterial(slot, r, g, b, 1f, metallic, roughness);
        NotifyAutosave();
    }

    public AttachmentInstance AddAttachment(LibraryAsset asset, AttachmentType type)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var attachment = _session.AddAttachmentFromGlb(asset.GlbPath, asset.Name, type, asset.Provenance);
        NotifyAutosave();
        return attachment;
    }

    public void AddFittedGarment(string fittedGlb, string presetName, ClippingReport? report = null)
    {
        _session.AddFittedGarment(fittedGlb, presetName, report);
        NotifyAutosave();
    }

    public async Task SaveProjectAsync(string path, CancellationToken cancellationToken = default)
    {
        var bundle = _session.Snapshot();
        bundle.Project.AppVersionLastSaved = "2.0.0";
        bundle.Project.ModifiedUtc = DateTime.UtcNow;
        await _projects.SaveAsync(bundle, path, cancellationToken);
        _session.MarkClean(path);
        _autosave?.AssociateMainFile(path);
    }

    public async Task OpenProjectAsync(string path, CancellationToken cancellationToken = default)
    {
        var bundle = await _projects.LoadAsync(path, cancellationToken);
        _session.LoadFrom(bundle, path);
        _autosave?.AssociateMainFile(path);
    }

    public string ExportActiveGlb(string destinationGlb)
    {
        var sceneRoot = Path.Combine(_workRoot, "compose-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sceneRoot);
        try
        {
            var parts = _session.MaterializeSceneGlbs(sceneRoot);
            if (parts.Count == 0)
                throw new InvalidOperationException("No active project mesh to export.");

            if (parts.Count == 1)
                return GlbExportService.Export(parts[0].GlbPath, destinationGlb);

            var composeInputs = parts
                .Select(p => (NodeName: p.Name, SourceGlb: p.GlbPath))
                .ToList();
            return GlbExportService.ComposeScenes(composeInputs, destinationGlb);
        }
        finally
        {
            try { Directory.Delete(sceneRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    public Task<AiEditExecutionResult> ApplyAiEditAsync(string prompt, CancellationToken cancellationToken = default) =>
        _edits.ExecutePromptAsync(prompt, stack: null, cancellationToken);

    public async Task FlushAutosaveAsync(CancellationToken cancellationToken = default)
    {
        if (_autosave is null)
            return;
        await _autosave.ScheduleAutosaveAsync(_session.Snapshot(), cancellationToken);
        await _autosave.FlushAsync(cancellationToken);
    }

    private void NotifyAutosave() => _autosave?.MarkDirty(_session.Snapshot());
}
