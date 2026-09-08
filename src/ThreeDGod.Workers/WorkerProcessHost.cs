using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ThreeDGod.Application;
using ThreeDGod.Core.Diagnostics;

namespace ThreeDGod.Workers;

public static class WorkerProtocol
{
    public const string Version = "3dgod-worker/1";
}

public sealed class WorkerProcessHost : IWorkerHost
{
    public int MaxLineBytes { get; }
    public IDiagnosticService? Diagnostics { get; }

    public WorkerProcessHost() : this(null, 1024 * 1024)
    {
    }

    public WorkerProcessHost(IDiagnosticService? diagnostics) : this(diagnostics, 1024 * 1024)
    {
    }

    public WorkerProcessHost(IDiagnosticService? diagnostics, int maxLineBytes)
    {
        Diagnostics = diagnostics;
        MaxLineBytes = maxLineBytes;
    }

    public async Task<WorkerRunResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        WorkerRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var jobId = request.JobId ?? Guid.NewGuid().ToString("N");
        var requestId = Guid.NewGuid().ToString("N");
        Diagnostics?.AddBreadcrumb(new DiagnosticBreadcrumb
        {
            Pipeline = "Worker.Run",
            Stage = "Start",
            Status = "Started",
            Provider = request.BackendId
        });

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

        Process process;
        try
        {
            process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start worker process.");
        }
        catch (Exception ex)
        {
            Diagnostics?.AddBreadcrumb(new DiagnosticBreadcrumb { Pipeline = "Worker.Run", Stage = "Start", Status = "Failed", Provider = request.BackendId });
            return new WorkerRunResult(false, null, "StartFailed", ex.Message, null, false, false, false);
        }

        using (process)
        {
            var stderr = new StringBuilder();
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stderr.AppendLine(e.Data);
            };
            process.BeginErrorReadLine();

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                var hello = await ReadMessageAsync(process.StandardOutput, cts.Token);
                if (hello is null)
                    return Fail("NoHello", "Worker did not send hello.", process, stderr, crashed: process.HasExited);

                var helloMsg = hello.Value;
                if (!string.Equals(helloMsg.GetString("type"), "hello", StringComparison.Ordinal))
                    return Fail("BadHello", "First message was not hello.", process, stderr, false);

                var payload = new Dictionary<string, object?>
                {
                    ["v"] = WorkerProtocol.Version,
                    ["type"] = "request",
                    ["id"] = requestId,
                    ["method"] = request.Method,
                    ["jobId"] = jobId,
                    ["params"] = JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(request.JsonParams) ? "{}" : request.JsonParams)
                };
                await WriteMessageAsync(process.StandardInput, payload, cts.Token);

                WorkerRunResult? result = null;
                while (!cts.Token.IsCancellationRequested)
                {
                    JsonElement? msg;
                    try
                    {
                        msg = await ReadMessageAsync(process.StandardOutput, cts.Token);
                    }
                    catch (WorkerProtocolException ex)
                    {
                        return Fail(ex.Code, ex.Message, process, stderr, process.HasExited);
                    }

                    if (msg is null)
                    {
                        await WaitExitAsync(process, TimeSpan.FromSeconds(2), CancellationToken.None);
                        return Fail("Crash", "Worker exited without result.", process, stderr, crashed: true);
                    }

                    var type = msg.Value.GetString("type");
                    var id = msg.Value.GetString("id");
                    if (type == "progress")
                        continue;
                    if (type == "result" && id == requestId)
                    {
                        result = new WorkerRunResult(true, msg.Value.GetRawText(), null, null, process.HasExited ? process.ExitCode : null, false, false, false);
                        break;
                    }
                    if (type == "error")
                    {
                        result = new WorkerRunResult(false, msg.Value.GetRawText(), msg.Value.GetString("code") ?? "WorkerError", msg.Value.GetString("message"), process.HasExited ? process.ExitCode : null, false, false, false);
                        break;
                    }
                }

                try
                {
                    await WriteMessageAsync(process.StandardInput, new Dictionary<string, object?> { ["v"] = WorkerProtocol.Version, ["type"] = "shutdown" }, CancellationToken.None);
                }
                catch (IOException)
                {
                    // Worker may already have exited.
                }

                if (result is not null)
                {
                    Diagnostics?.AddBreadcrumb(new DiagnosticBreadcrumb { Pipeline = "Worker.Run", Stage = "Result", Status = result.Ok ? "Completed" : "Failed", Provider = request.BackendId });
                    return result;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    TryKill(process);
                    return new WorkerRunResult(false, null, "Cancelled", "Worker request cancelled.", process.HasExited ? process.ExitCode : null, false, false, true);
                }

                TryKill(process);
                return new WorkerRunResult(false, null, "Timeout", "Worker timed out.", process.HasExited ? process.ExitCode : null, false, true, false);
            }
            catch (WorkerProtocolException ex)
            {
                TryKill(process);
                return Fail(ex.Code, ex.Message, process, stderr, process.HasExited);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                return new WorkerRunResult(false, null, "Cancelled", "Worker request cancelled.", process.HasExited ? process.ExitCode : null, false, false, true);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return new WorkerRunResult(false, null, "Timeout", "Worker timed out.", process.HasExited ? process.ExitCode : null, false, true, false);
            }
            catch (Exception ex)
            {
                Diagnostics?.Capture(ex, "WorkerProcessHost", jobId, jobId, request.BackendId);
                TryKill(process);
                return new WorkerRunResult(false, null, "HostError", ex.Message, process.HasExited ? process.ExitCode : null, process.HasExited, false, false);
            }
        }
    }

    private async Task<JsonElement?> ReadMessageAsync(StreamReader reader, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
                return null;
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] != '{')
                continue;
            if (Encoding.UTF8.GetByteCount(line) > MaxLineBytes)
                throw new WorkerProtocolException("OversizedLine", $"Worker line exceeded {MaxLineBytes} bytes.");
            try
            {
                return JsonSerializer.Deserialize<JsonElement>(trimmed);
            }
            catch (JsonException)
            {
                throw new WorkerProtocolException("InvalidJson", "Worker sent invalid JSON.");
            }
        }
    }

    private static async Task WriteMessageAsync(StreamWriter writer, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        await writer.WriteLineAsync(json.AsMemory(), ct);
        await writer.FlushAsync(ct);
    }

    private static WorkerRunResult Fail(string code, string message, Process process, StringBuilder stderr, bool crashed)
    {
        var exit = process.HasExited ? process.ExitCode : (int?)null;
        return new WorkerRunResult(false, stderr.Length == 0 ? null : stderr.ToString(), code, message, exit, crashed, false, false);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static async Task WaitExitAsync(Process process, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }
}

file sealed class WorkerProtocolException : Exception
{
    public string Code { get; }
    public WorkerProtocolException(string code, string message) : base(message) => Code = code;
}

file static class JsonElementExtensions
{
    public static string? GetString(this JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
