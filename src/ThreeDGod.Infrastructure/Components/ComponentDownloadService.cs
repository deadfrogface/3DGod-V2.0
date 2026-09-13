using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace ThreeDGod.Infrastructure.Components;

public sealed class ComponentDownloadProgress
{
    public long BytesReceived { get; init; }
    public long? TotalBytes { get; init; }
    public double? Percent => TotalBytes is > 0 ? 100.0 * BytesReceived / TotalBytes.Value : null;
}

public sealed class ComponentDownloadRequest
{
    public required Uri Url { get; init; }
    public required string DestinationPath { get; init; }
    public string? ExpectedSha256 { get; init; }
    public bool AllowResume { get; init; } = true;
    public IProgress<ComponentDownloadProgress>? Progress { get; init; }
}

public sealed class ComponentDownloadResult
{
    public required string Path { get; init; }
    public required string Sha256 { get; init; }
    public long BytesWritten { get; init; }
    public bool Resumed { get; init; }
}

public interface IComponentDownloadService
{
    Task<ComponentDownloadResult> DownloadAsync(ComponentDownloadRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Owned download backend for component packages. Uses HttpClient (not bezzad/Downloader)
/// so 3D God keeps URL pinning, SHA-256, resume, and error mapping behind one interface.
/// Large-file resume via HTTP Range when the server supports it.
/// </summary>
public sealed class ComponentDownloadService : IComponentDownloadService, IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsClient;

    public ComponentDownloadService(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            _http = new HttpClient { Timeout = TimeSpan.FromHours(6) };
            _ownsClient = true;
        }
        else
        {
            _http = httpClient;
            _ownsClient = false;
        }
    }

    public async Task<ComponentDownloadResult> DownloadAsync(ComponentDownloadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Url.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("InvalidUrl – only http/https downloads are allowed.");

        var dest = Path.GetFullPath(request.DestinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        var partial = dest + ".partial";

        long existing = 0;
        var resumed = false;
        if (request.AllowResume && File.Exists(partial))
            existing = new FileInfo(partial).Length;

        using var response = await SendWithOptionalRangeAsync(request.Url, existing, cancellationToken);

        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            existing = 0;
            resumed = false;
            if (File.Exists(partial))
                File.Delete(partial);
        }
        else if (response.StatusCode == HttpStatusCode.PartialContent && existing > 0)
        {
            resumed = true;
        }
        else if (existing > 0 && response.StatusCode == HttpStatusCode.OK)
        {
            existing = 0;
            resumed = false;
            if (File.Exists(partial))
                File.Delete(partial);
        }
        else if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"HttpFailure – HTTP {(int)response.StatusCode} from {request.Url.Host}.");
        }

        HttpResponseMessage effective = response;
        HttpResponseMessage? restart = null;
        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            restart = await SendWithOptionalRangeAsync(request.Url, 0, cancellationToken);
            effective = restart;
            if (!effective.IsSuccessStatusCode)
            {
                restart.Dispose();
                throw new InvalidOperationException($"HttpFailure – HTTP {(int)effective.StatusCode} from {request.Url.Host}.");
            }
        }

        try
        {
            var total = effective.Content.Headers.ContentLength is long len
                ? existing + len
                : (long?)null;

            await using var input = await effective.Content.ReadAsStreamAsync(cancellationToken);
            await using (var output = new FileStream(
                             partial,
                             existing > 0 ? FileMode.Append : FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             1024 * 128,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[1024 * 128];
                long received = existing;
                int read;
                while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    received += read;
                    request.Progress?.Report(new ComponentDownloadProgress
                    {
                        BytesReceived = received,
                        TotalBytes = total
                    });
                }
            }

            var sha = await Sha256FileAsync(partial, cancellationToken);
            if (!string.IsNullOrWhiteSpace(request.ExpectedSha256) &&
                !string.Equals(sha, request.ExpectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(partial); } catch { /* ignore */ }
                throw new InvalidOperationException("HashMismatch – downloaded file SHA-256 did not match the pinned digest.");
            }

            if (File.Exists(dest))
                File.Delete(dest);
            File.Move(partial, dest);

            return new ComponentDownloadResult
            {
                Path = dest,
                Sha256 = sha,
                BytesWritten = new FileInfo(dest).Length,
                Resumed = resumed
            };
        }
        finally
        {
            restart?.Dispose();
        }
    }

    private async Task<HttpResponseMessage> SendWithOptionalRangeAsync(Uri url, long existing, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        if (existing > 0)
            message.Headers.Range = new RangeHeaderValue(existing, null);

        try
        {
            return await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException("HttpFailure – request could not be sent: " + ex.Message, ex);
        }
    }

    public void Dispose()
    {
        if (_ownsClient)
            _http.Dispose();
    }

    private static async Task<string> Sha256FileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
