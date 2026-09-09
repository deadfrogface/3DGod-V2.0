namespace ThreeDGod.Core.Diagnostics;

public enum SymbolStatus
{
    Available,
    Unavailable
}

public sealed class SourceLocation
{
    public string? FilePath { get; init; }
    public string? Method { get; init; }
    public int? Line { get; init; }
    public int? Column { get; init; }
    public SymbolStatus SymbolStatus { get; init; } = SymbolStatus.Unavailable;
}

public sealed class StackFrameDiagnostic
{
    public string? TypeName { get; init; }
    public string? Method { get; init; }
    public string? FilePath { get; init; }
    public int? Line { get; init; }
    public int? Column { get; init; }
    public bool IsApplicationFrame { get; init; }
}

public sealed class CodeContext
{
    public string? FilePath { get; init; }
    public int StartLine { get; init; }
    public int ErrorLine { get; init; }
    public IReadOnlyList<string> Lines { get; init; } = [];
}

public sealed class DiagnosticBreadcrumb
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string Pipeline { get; init; } = "";
    public string Stage { get; init; } = "";
    public string Substage { get; init; } = "";
    public string Status { get; init; } = "Started";
    public string? Provider { get; init; }
}

public sealed class DiagnosticIssue
{
    public string ErrorCode { get; init; } = "UNHANDLED";
    public string ExceptionType { get; init; } = "";
    public string Message { get; init; } = "";
    public string Source { get; init; } = "";
    public string? CorrelationId { get; init; }
    public string? JobId { get; init; }
    public string? BackendId { get; init; }
    public SourceLocation Location { get; init; } = new();
    public CodeContext? CodeContext { get; init; }
    public IReadOnlyList<StackFrameDiagnostic> Stack { get; init; } = [];
    public IReadOnlyList<DiagnosticBreadcrumb> Breadcrumbs { get; init; } = [];
    public string? Pipeline { get; init; }
    public string? LastSuccessfulStage { get; init; }
    public string? FailingStage { get; init; }
}

public interface IDiagnosticService
{
    DiagnosticIssue Capture(Exception exception, string source, string? correlationId = null, string? jobId = null, string? backendId = null);
    void AddBreadcrumb(DiagnosticBreadcrumb breadcrumb);
    IReadOnlyList<DiagnosticIssue> Issues { get; }
    IReadOnlyList<DiagnosticBreadcrumb> Breadcrumbs { get; }
    void ClearBreadcrumbs();
}

public sealed class CSharpExceptionEnricher
{
    public DiagnosticIssue Enrich(
        Exception exception,
        string source,
        IReadOnlyList<DiagnosticBreadcrumb>? breadcrumbs = null,
        string? correlationId = null,
        string? jobId = null,
        string? backendId = null)
    {
        var trace = new System.Diagnostics.StackTrace(exception, fNeedFileInfo: true);
        var frames = new List<StackFrameDiagnostic>();
        SourceLocation? firstApp = null;

        foreach (var frame in trace.GetFrames() ?? [])
        {
            var method = frame.GetMethod();
            var typeName = method?.DeclaringType?.FullName;
            var methodName = method?.Name;
            var file = frame.GetFileName();
            var line = frame.GetFileLineNumber();
            var column = frame.GetFileColumnNumber();
            var ns = method?.DeclaringType?.Namespace ?? "";
            var isApp = ns.StartsWith("ThreeDGod", StringComparison.Ordinal) ||
                        ns.StartsWith("ThreeDGodCreator", StringComparison.Ordinal);

            var diag = new StackFrameDiagnostic
            {
                TypeName = typeName,
                Method = methodName,
                FilePath = string.IsNullOrWhiteSpace(file) ? null : file,
                Line = line > 0 ? line : null,
                Column = column > 0 ? column : null,
                IsApplicationFrame = isApp
            };
            frames.Add(diag);

            if (isApp && firstApp is null)
            {
                var symbols = !string.IsNullOrWhiteSpace(file) && line > 0
                    ? SymbolStatus.Available
                    : SymbolStatus.Unavailable;
                firstApp = new SourceLocation
                {
                    FilePath = symbols == SymbolStatus.Available ? file : file,
                    Method = methodName,
                    Line = symbols == SymbolStatus.Available ? line : null,
                    Column = symbols == SymbolStatus.Available ? (column > 0 ? column : null) : null,
                    SymbolStatus = symbols
                };
            }
        }

        var location = firstApp ?? new SourceLocation { SymbolStatus = SymbolStatus.Unavailable };
        CodeContext? context = null;
        if (location.SymbolStatus == SymbolStatus.Available &&
            location.FilePath is not null &&
            location.Line is int errorLine &&
            File.Exists(location.FilePath))
        {
            var all = File.ReadAllLines(location.FilePath);
            var start = Math.Max(1, errorLine - 5);
            var end = Math.Min(all.Length, errorLine + 5);
            var slice = new List<string>();
            for (var i = start; i <= end; i++)
                slice.Add(all[i - 1]);
            context = new CodeContext
            {
                FilePath = location.FilePath,
                StartLine = start,
                ErrorLine = errorLine,
                Lines = slice
            };
        }

        var crumbs = breadcrumbs?.ToArray() ?? [];
        var lastOk = crumbs.LastOrDefault(c => c.Status is "Completed" or "Fallback");
        var failing = crumbs.LastOrDefault(c => c.Status == "Failed");

        return new DiagnosticIssue
        {
            ErrorCode = "CS_EXCEPTION",
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            Source = source,
            CorrelationId = correlationId,
            JobId = jobId,
            BackendId = backendId,
            Location = location,
            CodeContext = context,
            Stack = frames,
            Breadcrumbs = crumbs,
            Pipeline = failing?.Pipeline ?? lastOk?.Pipeline ?? crumbs.LastOrDefault()?.Pipeline,
            LastSuccessfulStage = lastOk?.Stage,
            FailingStage = failing?.Stage
        };
    }
}

public sealed class DiagnosticService : IDiagnosticService
{
    private readonly CSharpExceptionEnricher _enricher = new();
    private readonly List<DiagnosticIssue> _issues = [];
    private readonly List<DiagnosticBreadcrumb> _breadcrumbs = [];
    private readonly object _gate = new();

    public IReadOnlyList<DiagnosticIssue> Issues
    {
        get { lock (_gate) return _issues.ToArray(); }
    }

    public IReadOnlyList<DiagnosticBreadcrumb> Breadcrumbs
    {
        get { lock (_gate) return _breadcrumbs.ToArray(); }
    }

    public void ClearBreadcrumbs()
    {
        lock (_gate)
            _breadcrumbs.Clear();
    }

    public void AddBreadcrumb(DiagnosticBreadcrumb breadcrumb)
    {
        lock (_gate)
            _breadcrumbs.Add(breadcrumb);
    }

    public DiagnosticIssue Capture(Exception exception, string source, string? correlationId = null, string? jobId = null, string? backendId = null)
    {
        DiagnosticBreadcrumb[] crumbs;
        lock (_gate)
            crumbs = _breadcrumbs.ToArray();
        var issue = _enricher.Enrich(exception, source, crumbs, correlationId, jobId, backendId);
        lock (_gate)
            _issues.Add(issue);
        return issue;
    }
}
