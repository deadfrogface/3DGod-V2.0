using System.Text;
using ThreeDGod.Core.Diagnostics;

namespace ThreeDGod.Core.Diagnostics;

public static class CursorReportBuilder
{
    public const string HeaderInstruction =
        "Fix the root cause. Do not hide the exception, add fake success, or remove validation. Preserve existing behavior and add/update a regression test.";

    public static string CreateCursorReport(DiagnosticIssue issue, string? stdoutTail = null, string? stderrTail = null, string? versions = null, string? hardware = null, string? expectedBehavior = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(HeaderInstruction);
        sb.AppendLine();
        sb.AppendLine($"Error Code: {Redact(issue.ErrorCode)}");
        sb.AppendLine($"Exception: {Redact(issue.ExceptionType)}: {Redact(issue.Message)}");
        sb.AppendLine($"Pipeline: {Redact(issue.Pipeline)}");
        sb.AppendLine($"Last Successful Stage: {Redact(issue.LastSuccessfulStage)}");
        sb.AppendLine($"Failing Stage: {Redact(issue.FailingStage)}");
        sb.AppendLine($"Backend/Worker: {Redact(issue.BackendId)}");
        sb.AppendLine($"Object: {Redact(issue.Source)}");
        if (issue.Scene is not null)
        {
            sb.AppendLine($"Scene.MeshAssetId: {issue.Scene.MeshAssetId}");
            sb.AppendLine($"Scene.CharacterId: {issue.Scene.CharacterId}");
            sb.AppendLine($"Scene.GarmentId: {issue.Scene.GarmentId}");
            sb.AppendLine($"Scene.RigId: {issue.Scene.RigId}");
            sb.AppendLine($"Scene.BoneId: {Redact(issue.Scene.BoneId)}");
            sb.AppendLine($"Scene.VertexIndices: {string.Join(',', issue.Scene.VertexIndices)}");
            sb.AppendLine($"Scene.TriangleIndices: {string.Join(',', issue.Scene.TriangleIndices)}");
        }
        sb.AppendLine($"Source File: {Redact(issue.Location.FilePath)}");
        sb.AppendLine($"Method: {Redact(issue.Location.Method)}");
        sb.AppendLine($"Line: {issue.Location.Line?.ToString() ?? "Unavailable"}");
        sb.AppendLine($"SymbolStatus: {issue.Location.SymbolStatus}");
        sb.AppendLine("Failing Code:");
        if (issue.CodeContext is { Lines.Count: > 0 })
        {
            var idx = issue.CodeContext.ErrorLine - issue.CodeContext.StartLine;
            if (idx >= 0 && idx < issue.CodeContext.Lines.Count)
                sb.AppendLine(issue.CodeContext.Lines[idx]);
        }
        else
        {
            sb.AppendLine("(no source context)");
        }
        sb.AppendLine("Code Context:");
        if (issue.CodeContext is { Lines.Count: > 0 })
        {
            var lineNo = issue.CodeContext.StartLine;
            foreach (var line in issue.CodeContext.Lines)
            {
                var mark = lineNo == issue.CodeContext.ErrorLine ? ">" : " ";
                sb.AppendLine($"{mark}{lineNo,4}| {line}");
                lineNo++;
            }
        }
        sb.AppendLine("Relevant Stack Trace:");
        foreach (var frame in issue.Stack.Where(f => f.IsApplicationFrame).Take(12))
            sb.AppendLine($"  at {frame.TypeName}.{frame.Method} in {frame.FilePath}:line {frame.Line}");
        sb.AppendLine($"stdout: {Redact(stdoutTail)}");
        sb.AppendLine($"stderr: {Redact(stderrTail)}");
        sb.AppendLine($"Versions: {Redact(versions)}");
        sb.AppendLine($"Hardware: {Redact(hardware)}");
        sb.AppendLine($"Expected Behavior: {Redact(expectedBehavior)}");
        sb.AppendLine($"CorrelationId: {Redact(issue.CorrelationId)}");
        sb.AppendLine($"JobId: {Redact(issue.JobId)}");
        return sb.ToString();
    }

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        var redacted = value;
        redacted = System.Text.RegularExpressions.Regex.Replace(redacted, "(?i)Bearer\\s+\\S+", "Bearer ***");
        redacted = System.Text.RegularExpressions.Regex.Replace(
            redacted,
            "(?i)(api[_-]?key|token|authorization|secret|password)\\s*[:=]\\s*\\S+",
            "$1=***");
        return redacted;
    }
}
