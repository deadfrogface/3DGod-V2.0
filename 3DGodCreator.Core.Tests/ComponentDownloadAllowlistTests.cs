using ThreeDGod.Infrastructure.Components;

namespace ThreeDGodCreator.Core.Tests;

public class ComponentDownloadAllowlistTests
{
    [Theory]
    [InlineData("https://github.com/astral-sh/uv/releases/download/x/y.zip", true)]
    [InlineData("https://huggingface.co/LocalAI-io/SkinTokens-GGUF/resolve/main/F16/x", true)]
    [InlineData("https://evil.example/malware.bin", false)]
    [InlineData("file:///tmp/x", false)]
    public void Allowlist_BlocksNonApprovedHosts(string url, bool allowed)
    {
        Assert.Equal(allowed, ComponentDownloadAllowlist.IsAllowed(new Uri(url)));
    }
}
