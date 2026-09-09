using System.Reflection;

namespace ThreeDGod.Infrastructure;

/// <summary>
/// Velopack update channel wiring. Models are never bundled in the base installer.
/// </summary>
public sealed class VelopackUpdateChannel
{
    public const string DefaultChannel = "stable";
    public const string PackId = "ThreeDGodCreator";

    public string Channel { get; init; } = DefaultChannel;
    public string AppVersion { get; init; } = "0.0.0";
    public string? FeedUrl { get; init; }

    public static VelopackUpdateChannel FromEnvironment()
    {
        var channel = Environment.GetEnvironmentVariable("THREEDGOD_UPDATE_CHANNEL");
        if (string.IsNullOrWhiteSpace(channel))
            channel = DefaultChannel;

        var feed = Environment.GetEnvironmentVariable("THREEDGOD_UPDATE_FEED_URL");
        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "0.0.0";

        return new VelopackUpdateChannel
        {
            Channel = channel.Trim().ToLowerInvariant(),
            AppVersion = version.Split('+')[0],
            FeedUrl = string.IsNullOrWhiteSpace(feed) ? null : feed.Trim()
        };
    }
}
