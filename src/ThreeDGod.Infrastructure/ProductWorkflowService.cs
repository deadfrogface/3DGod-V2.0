using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
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

    public ProductWorkflowService(
        ActiveProjectSession session,
        IProjectService projects,
        AllowlistedAiEditExecutor edits,
        AutosaveService? autosave = null,
        string? workRoot = null)
    {
        _session = session;
        _projects = projects;
        _edits = edits;
        _autosave = autosave;
        _workRoot = workRoot ?? Path.Combine(Path.GetTempPath(), "3dgod-product-workflow");
        Directory.CreateDirectory(_workRoot);
    }

    public ActiveProjectSession Session => _session;

    public void NewHumanProject(string name = "Human")
    {
        _session.NewProject(name);
        _autosave?.AssociateMainFile(null);
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
        var src = _session.GetActiveMeshGlbPathOrMaterialize(Path.Combine(_workRoot, "materialize"));
        if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
            throw new InvalidOperationException("No active project mesh to export.");
        return GlbExportService.Export(src, destinationGlb);
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
