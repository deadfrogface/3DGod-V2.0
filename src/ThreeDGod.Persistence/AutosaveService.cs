using ThreeDGod.Core.Domain;

namespace ThreeDGod.Persistence;

public sealed class RecoverySession
{
    public string SessionId { get; init; } = "";
    public string FilePath { get; init; } = "";
    public DateTime SavedUtc { get; init; }
    public Guid? ProjectId { get; init; }
    public string ProjectName { get; init; } = "";
}

public sealed class AutosaveService
{
    private readonly GodProjectArchive _archive;
    private readonly TimeSpan _debounce;
    private readonly object _gate = new();
    private ProjectBundle? _pending;
    private string? _mainProjectPath;
    private CancellationTokenSource? _debounceCts;
    private bool _dirty;

    public AutosaveService(string recoveryRoot, TimeSpan? debounce = null, GodProjectArchive? archive = null)
    {
        RecoveryRoot = recoveryRoot;
        _debounce = debounce ?? TimeSpan.FromSeconds(2);
        _archive = archive ?? new GodProjectArchive();
        Directory.CreateDirectory(RecoveryRoot);
    }

    public string RecoveryRoot { get; }

    public bool IsDirty
    {
        get { lock (_gate) return _dirty; }
    }

    public void AssociateMainFile(string? mainProjectPath)
    {
        lock (_gate)
            _mainProjectPath = string.IsNullOrWhiteSpace(mainProjectPath) ? null : Path.GetFullPath(mainProjectPath);
    }

    public void MarkDirty(ProjectBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        lock (_gate)
        {
            _pending = Clone(bundle);
            _dirty = true;
        }
    }

    public async Task ScheduleAutosaveAsync(ProjectBundle bundle, CancellationToken cancellationToken = default)
    {
        MarkDirty(bundle);
        CancellationTokenSource cts;
        lock (_gate)
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            cts = _debounceCts;
        }

        try
        {
            await Task.Delay(_debounce, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await FlushAsync(cancellationToken);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        ProjectBundle bundle;
        lock (_gate)
        {
            if (!_dirty || _pending is null)
                return;
            bundle = Clone(_pending);
        }

        var sessionId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N")[..8];
        var path = Path.Combine(RecoveryRoot, sessionId + ".3dgod");
        lock (_gate)
        {
            if (_mainProjectPath is not null &&
                string.Equals(Path.GetFullPath(path), _mainProjectPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ProjectArchiveException("AutosaveOverwrite", "Autosave refused to overwrite the main project file.");
            }
        }

        await _archive.SaveAsync(bundle, path, cancellationToken);

        lock (_gate)
        {
            _dirty = false;
        }
    }

    public IReadOnlyList<RecoverySession> ListRecoveries()
    {
        if (!Directory.Exists(RecoveryRoot))
            return [];

        var list = new List<RecoverySession>();
        foreach (var file in Directory.GetFiles(RecoveryRoot, "*.3dgod"))
        {
            try
            {
                var bundle = _archive.LoadAsync(file).GetAwaiter().GetResult();
                list.Add(new RecoverySession
                {
                    SessionId = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    SavedUtc = File.GetLastWriteTimeUtc(file),
                    ProjectId = bundle.Project.ProjectId,
                    ProjectName = bundle.Project.Name
                });
            }
            catch (ProjectArchiveException)
            {
                // Skip unreadable recovery files; do not hide the exception by treating them as success.
                list.Add(new RecoverySession
                {
                    SessionId = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    SavedUtc = File.GetLastWriteTimeUtc(file),
                    ProjectName = "(unreadable)"
                });
            }
        }

        return list.OrderByDescending(s => s.SavedUtc).ToList();
    }

    public Task<ProjectBundle> RestoreAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = ListRecoveries().FirstOrDefault(s => s.SessionId == sessionId)
            ?? throw new ProjectArchiveException("RecoveryMissing", $"Recovery session not found: {sessionId}");
        return _archive.LoadAsync(session.FilePath, cancellationToken);
    }

    public void Discard(string sessionId)
    {
        var session = ListRecoveries().FirstOrDefault(s => s.SessionId == sessionId);
        if (session is null)
            return;
        File.Delete(session.FilePath);
        var bak = session.FilePath + ".bak";
        if (File.Exists(bak))
            File.Delete(bak);
    }

    private static ProjectBundle Clone(ProjectBundle bundle) =>
        DomainJson.Deserialize<ProjectBundle>(DomainJson.Serialize(bundle));
}
