using System.Text;

namespace ThreeDGodCreator.Core.Services;

/// <summary>
/// Logs startup and error messages to a file and provides formatted output for console/debug.
/// Ensures no failure goes unreported.
/// </summary>
public static class StartupLogger
{
    private static readonly object _lock = new();
    private static string _logPath = "";
    private static readonly List<string> _sessionLog = new();

    /// <summary>
    /// Initialize the logger. Call once at app startup.
    /// </summary>
    public static void Initialize()
    {
        _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
        _sessionLog.Clear();
        lock (_lock)
        {
            var header = $"\n========== Session: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========\n";
            File.AppendAllText(_logPath, header);
        }
    }

    /// <summary>
    /// Log a successful step with checkmark.
    /// </summary>
    public static void StepOk(int stepNumber, string stepName, string? detail = null)
    {
        var msg = detail != null
            ? $"Step {stepNumber}: {stepName} OK ({detail})"
            : $"Step {stepNumber}: {stepName} OK";
        Write(msg, isError: false);
    }

    /// <summary>
    /// Log a failed step with clear error indication.
    /// </summary>
    public static void StepFail(int stepNumber, string stepName, string reason, string? suggestedFix = null)
    {
        var msg = $"Step {stepNumber}: {stepName} FAIL - {reason}";
        if (!string.IsNullOrEmpty(suggestedFix))
            msg += $"{Environment.NewLine}    -> {suggestedFix}";
        Write(msg, isError: true);
    }

    /// <summary>
    /// Log a warning (step passed but with caveats).
    /// </summary>
    public static void StepWarn(int stepNumber, string stepName, string message)
    {
        var msg = $"Step {stepNumber}: {stepName} WARN - {message}";
        Write(msg, isError: false);
    }

    /// <summary>
    /// Write raw message to log file and session.
    /// </summary>
    public static void Write(string message, bool isError = false)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {(isError ? "ERROR: " : "")}{message}";
        lock (_lock)
        {
            _sessionLog.Add(line);
            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch
            {
                // Cannot log to file; at least keep in memory
            }
        }
    }

    /// <summary>
    /// Get all messages from this session for display.
    /// </summary>
    public static string GetSessionSummary()
    {
        lock (_lock)
        {
            return string.Join(Environment.NewLine, _sessionLog);
        }
    }

    /// <summary>
    /// Get the path to the log file.
    /// </summary>
    public static string GetLogFilePath() => _logPath;

    /// <summary>
    /// Log an exception with full details.
    /// </summary>
    public static void LogException(Exception ex, string context = "")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"EXCEPTION in {context}:");
        sb.AppendLine(ex.Message);
        sb.AppendLine(ex.StackTrace ?? "");
        if (ex.InnerException != null)
        {
            sb.AppendLine("--- Inner ---");
            sb.AppendLine(ex.InnerException.Message);
            sb.AppendLine(ex.InnerException.StackTrace ?? "");
        }
        Write(sb.ToString(), isError: true);
    }
}
