using System.Net;
using System.Security.Cryptography;
using System.Text;
using ThreeDGod.Infrastructure.Components;

namespace ThreeDGodCreator.Core.Tests;

public class ComponentDownloadServiceTests
{
    [Fact]
    public async Task DownloadAsync_WritesFile_AndValidatesSha256()
    {
        var payload = Encoding.UTF8.GetBytes("3dgod-download-ok");
        var sha = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        using var http = new HttpClient(new StubHandler(payload));
        using var svc = new ComponentDownloadService(http);
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-dl-" + Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            var result = await svc.DownloadAsync(new ComponentDownloadRequest
            {
                Url = new Uri("https://example.test/pkg.bin"),
                DestinationPath = dest,
                ExpectedSha256 = sha
            });
            Assert.True(File.Exists(result.Path));
            Assert.Equal(sha, result.Sha256);
            Assert.Equal(payload.Length, result.BytesWritten);
            Assert.False(result.Resumed);
        }
        finally
        {
            try { File.Delete(dest); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task DownloadAsync_Cancels()
    {
        using var http = new HttpClient(new SlowHandler());
        using var svc = new ComponentDownloadService(http);
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-dl-cancel-" + Guid.NewGuid().ToString("N") + ".bin");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            svc.DownloadAsync(new ComponentDownloadRequest
            {
                Url = new Uri("https://example.test/slow.bin"),
                DestinationPath = dest
            }, cts.Token));
    }

    [Fact]
    public async Task DownloadAsync_HashMismatch_DeletesPartial()
    {
        var payload = Encoding.UTF8.GetBytes("mismatch");
        using var http = new HttpClient(new StubHandler(payload));
        using var svc = new ComponentDownloadService(http);
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-dl-hash-" + Guid.NewGuid().ToString("N") + ".bin");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DownloadAsync(new ComponentDownloadRequest
            {
                Url = new Uri("https://example.test/pkg.bin"),
                DestinationPath = dest,
                ExpectedSha256 = new string('a', 64)
            }));
        Assert.Contains("HashMismatch", ex.Message);
        Assert.False(File.Exists(dest));
        Assert.False(File.Exists(dest + ".partial"));
    }

    [Fact]
    public async Task DownloadAsync_HttpFailure()
    {
        using var http = new HttpClient(new StubHandler(Array.Empty<byte>(), HttpStatusCode.NotFound));
        using var svc = new ComponentDownloadService(http);
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-dl-404-" + Guid.NewGuid().ToString("N") + ".bin");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DownloadAsync(new ComponentDownloadRequest
            {
                Url = new Uri("https://example.test/missing.bin"),
                DestinationPath = dest
            }));
        Assert.Contains("HttpFailure", ex.Message);
    }

    [Fact]
    public async Task DownloadAsync_InvalidScheme_Rejected()
    {
        using var svc = new ComponentDownloadService(new HttpClient());
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-dl-bad-" + Guid.NewGuid().ToString("N") + ".bin");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DownloadAsync(new ComponentDownloadRequest
            {
                Url = new Uri("file:///tmp/x.bin"),
                DestinationPath = dest
            }));
        Assert.Contains("InvalidUrl", ex.Message);
    }

    [Fact]
    public async Task DownloadAsync_ResumesPartial_WhenServerSupportsRange()
    {
        var full = Encoding.UTF8.GetBytes("ABCDEFGHIJKLMNOP");
        var dest = Path.Combine(Path.GetTempPath(), "3dgod-dl-resume-" + Guid.NewGuid().ToString("N") + ".bin");
        var partial = dest + ".partial";
        await File.WriteAllBytesAsync(partial, full.AsSpan(0, 4).ToArray());
        using var http = new HttpClient(new RangeHandler(full));
        using var svc = new ComponentDownloadService(http);
        try
        {
            var result = await svc.DownloadAsync(new ComponentDownloadRequest
            {
                Url = new Uri("https://example.test/resume.bin"),
                DestinationPath = dest,
                AllowResume = true
            });
            Assert.True(result.Resumed);
            Assert.Equal(full, await File.ReadAllBytesAsync(result.Path));
        }
        finally
        {
            try { File.Delete(dest); } catch { }
            try { File.Delete(partial); } catch { }
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly byte[] _body;
        private readonly HttpStatusCode _code;
        public StubHandler(byte[] body, HttpStatusCode code = HttpStatusCode.OK)
        {
            _body = body;
            _code = code;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_code)
            {
                Content = new ByteArrayContent(_body)
            };
            response.Content.Headers.ContentLength = _body.Length;
            return Task.FromResult(response);
        }
    }

    private sealed class SlowHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class RangeHandler : HttpMessageHandler
    {
        private readonly byte[] _full;
        public RangeHandler(byte[] full) => _full = full;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Range?.Ranges.FirstOrDefault() is { From: long from })
            {
                var slice = _full.AsSpan((int)from).ToArray();
                var response = new HttpResponseMessage(HttpStatusCode.PartialContent)
                {
                    Content = new ByteArrayContent(slice)
                };
                response.Content.Headers.ContentLength = slice.Length;
                return Task.FromResult(response);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_full)
            });
        }
    }
}
