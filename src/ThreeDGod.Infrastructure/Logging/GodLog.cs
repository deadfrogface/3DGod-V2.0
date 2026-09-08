using System.Diagnostics;
using System.Reflection;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace ThreeDGod.Infrastructure.Logging;

public sealed class WorkerLoggedException : Exception
{
    public string CorrelationId { get; }
    public string? JobId { get; }
    public string? BackendId { get; }

    public WorkerLoggedException(string userMessage, Exception inner, string correlationId, string? jobId, string? backendId)
        : base(userMessage, inner)
    {
        CorrelationId = correlationId;
        JobId = jobId;
        BackendId = backendId;
    }
}

public static class GodLog
{
    private static readonly object Gate = new();
    private static bool _initialized;
    public static string AppVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

    public static string LogDirectory { get; private set; } = DefaultLogDirectory();

    public static void Initialize(string? logDirectory = null)
    {
        lock (Gate)
        {
            Log.CloseAndFlush();
            LogDirectory = logDirectory ?? DefaultLogDirectory();
            Directory.CreateDirectory(LogDirectory);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("AppVersion", AppVersion)
                .WriteTo.File(
                    path: Path.Combine(LogDirectory, "3dgod-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromMilliseconds(50),
                    outputTemplate: "{Timestamp:o} [{Level:u3}] app={AppVersion} corr={CorrelationId} job={JobId} backend={BackendId} durationMs={DurationMs} result={Result} errorCode={ErrorCode} {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            _initialized = true;
            Log.Information("GodLog initialized at {LogDirectory}", LogDirectory);
        }
    }

    public static void Write(
        string message,
        string? correlationId = null,
        string? jobId = null,
        string? backendId = null,
        LogEventLevel level = LogEventLevel.Information,
        long? durationMs = null,
        string? result = null,
        string? errorCode = null)
    {
        EnsureInit();
        using (LogContext.PushProperty("CorrelationId", correlationId ?? ""))
        using (LogContext.PushProperty("JobId", jobId ?? ""))
        using (LogContext.PushProperty("BackendId", backendId ?? ""))
        using (LogContext.PushProperty("DurationMs", durationMs))
        using (LogContext.PushProperty("Result", result ?? ""))
        using (LogContext.PushProperty("ErrorCode", errorCode ?? ""))
        {
            Log.Write(level, "{Message}", message);
        }
    }

    /// <summary>
    /// Logs a worker exception and throws a user-facing wrapper. Never swallows the error.
    /// </summary>
    public static void ThrowWorkerException(Exception exception, string? correlationId = null, string? jobId = null, string? backendId = null)
    {
        EnsureInit();
        var corr = correlationId ?? Guid.NewGuid().ToString("N");
        using (LogContext.PushProperty("CorrelationId", corr))
        using (LogContext.PushProperty("JobId", jobId ?? ""))
        using (LogContext.PushProperty("BackendId", backendId ?? ""))
        using (LogContext.PushProperty("Result", "error"))
        using (LogContext.PushProperty("ErrorCode", exception.GetType().Name))
        {
            Log.Error(exception, "Worker exception");
        }

        var user = $"Worker-Fehler ({exception.GetType().Name}): {exception.Message}. Details im Log-Ordner.";
        throw new WorkerLoggedException(user, exception, corr, jobId, backendId);
    }

    public static void LogCrash(Exception exception, string source)
    {
        EnsureInit();
        Log.Fatal(exception, "Crash handler: {Source}", source);
        Log.CloseAndFlush();
        _initialized = false;
    }

    public static void OpenLogFolder()
    {
        EnsureInit();
        Directory.CreateDirectory(LogDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = LogDirectory,
            UseShellExecute = true
        });
    }

    public static void Close()
    {
        lock (Gate)
        {
            Log.CloseAndFlush();
            _initialized = false;
        }
    }

    private static void EnsureInit()
    {
        if (!_initialized)
            Initialize();
    }

    private static string DefaultLogDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "3DGod", "Logs");
}
