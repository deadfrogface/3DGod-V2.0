using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ThreeDGod.Application;

namespace ThreeDGod.Workers;

public sealed class WorkerSession : IAsyncDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;
    private readonly StringBuilder _stderr = new();
    private readonly int _maxLineBytes;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    private WorkerSession(Process process, StreamWriter stdin, StreamReader stdout, int maxLineBytes)
    {
        _process = process;
        _stdin = stdin;
        _stdout = stdout;
        _maxLineBytes = maxLineBytes;
        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                _stderr.AppendLine(e.Data);
        };
        _process.BeginErrorReadLine();
    }

    public static async Task<WorkerSession> StartAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan helloTimeout,
        int maxLineBytes = 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        psi.Environment["PYTHONUNBUFFERED"] = "1";
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start worker process.");
        var session = new WorkerSession(process, process.StandardInput, process.StandardOutput, maxLineBytes);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(helloTimeout);
        var hello = await session.ReadMessageAsync(cts.Token);
        if (hello is null || !string.Equals(hello.Value.GetString("type"), "hello", StringComparison.Ordinal))
        {
            await session.DisposeAsync();
            throw new InvalidOperationException("Worker did not send hello.");
        }
        return session;
    }

    public async Task<WorkerRunResult> RequestAsync(WorkerRequest request, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_disposed || _process.HasExited)
                return new WorkerRunResult(false, _stderr.ToString(), "Crash", "Worker process is not running.", _process.HasExited ? _process.ExitCode : null, true, false, false);

            var jobId = request.JobId ?? Guid.NewGuid().ToString("N");
            var requestId = Guid.NewGuid().ToString("N");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            var payload = new Dictionary<string, object?>
            {
                ["v"] = WorkerProtocol.Version,
                ["type"] = "request",
                ["id"] = requestId,
                ["method"] = request.Method,
                ["jobId"] = jobId,
                ["params"] = JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(request.JsonParams) ? "{}" : request.JsonParams)
            };
            await WriteMessageAsync(_stdin, payload, cts.Token);

            while (!cts.Token.IsCancellationRequested)
            {
                var msg = await ReadMessageAsync(cts.Token);
                if (msg is null)
                    return new WorkerRunResult(false, _stderr.ToString(), "Crash", "Worker exited without result.", _process.HasExited ? _process.ExitCode : null, true, false, false);
                var type = msg.Value.GetString("type");
                var id = msg.Value.GetString("id");
                if (type == "progress")
                    continue;
                if (type == "result" && id == requestId)
                    return new WorkerRunResult(true, msg.Value.GetRawText(), null, null, null, false, false, false);
                if (type == "error")
                    return new WorkerRunResult(false, msg.Value.GetRawText(), msg.Value.GetString("code") ?? "WorkerError", msg.Value.GetString("message"), null, false, false, false);
            }

            return new WorkerRunResult(false, null, "Timeout", "Worker timed out.", null, false, true, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new WorkerRunResult(false, null, "Cancelled", "Worker request cancelled.", null, false, false, true);
        }
        catch (OperationCanceledException)
        {
            return new WorkerRunResult(false, null, "Timeout", "Worker timed out.", null, false, true, false);
        }
        catch (Exception ex)
        {
            return new WorkerRunResult(false, _stderr.ToString(), "HostError", ex.Message, _process.HasExited ? _process.ExitCode : null, _process.HasExited, false, false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        try
        {
            await WriteMessageAsync(_stdin, new Dictionary<string, object?> { ["v"] = WorkerProtocol.Version, ["type"] = "shutdown" }, CancellationToken.None);
        }
        catch (IOException)
        {
        }
        try
        {
            if (!_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
        _process.Dispose();
        _gate.Dispose();
    }

    private async Task<JsonElement?> ReadMessageAsync(CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await _stdout.ReadLineAsync(ct);
            if (line is null)
                return null;
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] != '{')
                continue;
            if (Encoding.UTF8.GetByteCount(line) > _maxLineBytes)
                throw new InvalidOperationException("Worker line exceeded limit.");
            return JsonSerializer.Deserialize<JsonElement>(trimmed);
        }
    }

    private static async Task WriteMessageAsync(StreamWriter writer, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        await writer.WriteLineAsync(json.AsMemory(), ct);
        await writer.FlushAsync(ct);
    }
}

file static class WorkerSessionJsonExtensions
{
    public static string? GetString(this JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
