using System.Text.Json;
using ThreeDGod.Application;

namespace ThreeDGod.Infrastructure;

public static class StabilityLicense
{
    public const string ProfileId = "stability-community";

    public static bool IsAccepted(string backendId)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod", "Models", backendId, "license.json");
        if (!File.Exists(path))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var accepted = root.TryGetProperty("accepted", out var a) && a.ValueKind == JsonValueKind.True;
            var id = root.TryGetProperty("id", out var i) ? i.GetString() : "";
            return accepted && string.Equals(id, ProfileId, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }
}

public sealed class ImageTo3DProfile
{
    public string Name { get; init; } = "";
    public int MinVramMb { get; init; }
}

public static class ImageTo3DProfiles
{
    public static IReadOnlyList<ImageTo3DProfile> For(string backendId) => backendId switch
    {
        "spar3d" => [new() { Name = "normal", MinVramMb = 12288 }, new() { Name = "low-vram", MinVramMb = 6144 }],
        "sf3d" => [new() { Name = "default", MinVramMb = 8192 }, new() { Name = "cpu-fallback", MinVramMb = 0 }],
        "trellis" => [new() { Name = "optional", MinVramMb = 12288 }],
        _ => [new() { Name = "default", MinVramMb = 0 }]
    };

    public static ImageTo3DProfile? Select(string backendId, int availableVramMb, bool cuda)
    {
        foreach (var profile in For(backendId).OrderByDescending(p => p.MinVramMb))
        {
            // Explicit CPU profiles (MinVramMb == 0): TripoSR and SF3D cpu-fallback.
            if (profile.MinVramMb == 0 && !cuda && backendId is "triposr" or "sf3d")
                return profile;
            if (cuda && availableVramMb >= profile.MinVramMb)
                return profile;
        }
        return null;
    }
}
