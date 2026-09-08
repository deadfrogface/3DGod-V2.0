using System.Text.Json;

namespace ThreeDGod.Workers;

public sealed class WorkerSmartDiagnostics
{
    public string? ExceptionType { get; init; }
    public string? Traceback { get; init; }
    public string? File { get; init; }
    public string? Function { get; init; }
    public int? Line { get; init; }
    public string? Code { get; init; }
    public string? Stage { get; init; }
    public string? LastSuccessfulStage { get; init; }
    public string? StderrTail { get; init; }
    public int? ExitCode { get; init; }
    public string? ActiveRequest { get; init; }
    public string? WorkerVersion { get; init; }

    public static WorkerSmartDiagnostics? TryParse(string? jsonPayload)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(jsonPayload);
            if (!doc.RootElement.TryGetProperty("diagnostics", out var d))
                return FromTopLevel(doc.RootElement);
            return new WorkerSmartDiagnostics
            {
                ExceptionType = Str(d, "exceptionType"),
                Traceback = Str(d, "traceback"),
                File = Str(d, "file"),
                Function = Str(d, "function"),
                Line = d.TryGetProperty("line", out var line) && line.TryGetInt32(out var n) ? n : null,
                Code = Str(d, "code"),
                Stage = Str(d, "stage"),
                LastSuccessfulStage = Str(d, "lastSuccessfulStage"),
                StderrTail = Str(d, "stderrTail")
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static WorkerSmartDiagnostics FromCrash(int exitCode, string? stderrTail, string? lastBreadcrumb, string? activeRequest, string? workerVersion) =>
        new()
        {
            ExitCode = exitCode,
            StderrTail = stderrTail,
            LastSuccessfulStage = lastBreadcrumb,
            ActiveRequest = activeRequest,
            WorkerVersion = workerVersion,
            Stage = "process.exit"
        };

    private static WorkerSmartDiagnostics FromTopLevel(JsonElement el) =>
        new()
        {
            ExceptionType = Str(el, "exceptionType"),
            Stage = Str(el, "stage")
        };

    private static string? Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
