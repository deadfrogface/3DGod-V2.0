using ThreeDGod.Core.Domain;

namespace ThreeDGod.Persistence;

public sealed class AnnyPresetStore
{
    public string BuiltInDirectory { get; }
    public string UserDirectory { get; }

    public AnnyPresetStore(string? repoRoot = null)
    {
        var root = repoRoot ?? FindRepoRoot();
        BuiltInDirectory = Path.Combine(root, "presets", "anny");
        UserDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod", "Presets", "anny");
    }

    public IReadOnlyList<AnnyHumanPreset> List()
    {
        var list = new List<AnnyHumanPreset>();
        list.AddRange(ReadDir(BuiltInDirectory, builtIn: true));
        list.AddRange(ReadDir(UserDirectory, builtIn: false));
        return list;
    }

    public AnnyHumanPreset Load(string name)
    {
        var user = Path.Combine(UserDirectory, name + ".json");
        if (File.Exists(user))
            return ReadFile(user, false);
        var builtIn = Path.Combine(BuiltInDirectory, name + ".json");
        if (File.Exists(builtIn))
            return ReadFile(builtIn, true);
        throw new FileNotFoundException($"Anny preset not found: {name}");
    }

    public string SaveUser(AnnyHumanPreset preset)
    {
        if (string.IsNullOrWhiteSpace(preset.Name))
            throw new InvalidOperationException("Preset name is required.");
        if (!string.Equals(preset.Backend, "anny", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only backend=anny presets are supported.");
        Directory.CreateDirectory(UserDirectory);
        preset.BuiltIn = false;
        var path = Path.Combine(UserDirectory, Sanitize(preset.Name) + ".json");
        File.WriteAllText(path, DomainJson.Serialize(preset));
        return path;
    }

    private static IEnumerable<AnnyHumanPreset> ReadDir(string dir, bool builtIn)
    {
        if (!Directory.Exists(dir))
            yield break;
        foreach (var file in Directory.GetFiles(dir, "*.json"))
            yield return ReadFile(file, builtIn);
    }

    private static AnnyHumanPreset ReadFile(string path, bool builtIn)
    {
        var preset = DomainJson.Deserialize<AnnyHumanPreset>(File.ReadAllText(path));
        if (string.IsNullOrWhiteSpace(preset.Name))
            preset.Name = Path.GetFileNameWithoutExtension(path);
        preset.BuiltIn = builtIn;
        return preset;
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "3DGodCreator.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }
}
