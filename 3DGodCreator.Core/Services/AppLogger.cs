using System.IO;
using System.Text;

namespace ThreeDGodCreator.Core.Services;

/// <summary>
/// Centralized logger that writes to file with immediate flush.
/// Logs persist even on app crash - uses AutoFlush and process exit handler.
/// </summary>
public static class AppLogger
{
    private static readonly object _lock = new();
    private static StreamWriter? _writer;
    private static string _logPath = "";
    private static bool _initialized;

    /// <summary>
    /// Initialize logger. Call at app startup.
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
            try
            {
                var lastShutdown = ReadLastShutdownReason();
                _writer = new StreamWriter(_logPath, append: true, Encoding.UTF8) { AutoFlush = true };
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                _writer.WriteLine();
                _writer.WriteLine($"========== Session: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
                if (!string.IsNullOrEmpty(lastShutdown))
                    _writer.WriteLine($"[Startup] Last shutdown: {lastShutdown}");
                _writer.WriteLine($"[Log] Log file write verified at {_logPath}");
                _writer.Flush();
                _initialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppLogger init failed: {ex.Message}");
            }
        }
    }

    private static string? ReadLastShutdownReason()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".last_shutdown.txt");
            if (File.Exists(path))
            {
                var s = File.ReadAllText(path).Trim();
                File.Delete(path);
                return s;
            }
        }
        catch { }
        return null;
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            try
            {
                var shutdownPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".last_shutdown.txt");
                var reason = File.Exists(shutdownPath) ? File.ReadAllText(shutdownPath).Trim() : "Normal exit";
                _writer?.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Process exiting. Reason: {reason}");
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
                if (!File.Exists(shutdownPath))
                    File.WriteAllText(shutdownPath, "Normal exit");
            }
            catch { }
        }
    }

    /// <summary>
    /// Write a pre-formatted line as-is. Flushes immediately.
    /// </summary>
    public static void WriteLine(string line)
    {
        if (!_initialized) Initialize();
        lock (_lock)
        {
            try
            {
                _writer?.WriteLine(line);
                _writer?.Flush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppLogger write failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Write a message with timestamp. Flushes immediately.
    /// </summary>
    public static void Write(string message, bool isError = false)
    {
        if (!_initialized) Initialize();
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {(isError ? "ERROR: " : "")}{message}";
        lock (_lock)
        {
            try
            {
                _writer?.WriteLine(line);
                _writer?.Flush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppLogger write failed: {ex.Message}");
            }
        }
    }

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

    public static string GetLogFilePath() => _logPath;

    public static void SetShutdownReason(string reason)
    {
        try
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".last_shutdown.txt"), reason);
        }
        catch { }
    }
}
