using ThreeDGod.Core.Domain;

namespace ThreeDGod.Persistence;

public sealed class AssetLibrary
{
    public string Root { get; }

    public AssetLibrary(string? root = null)
    {
        Root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DGod", "Library");
        Directory.CreateDirectory(Root);
    }

    public string Save(LibraryAsset asset)
    {
        var dir = Path.Combine(Root, asset.Category, asset.LibraryAssetId.ToString("D"));
        Directory.CreateDirectory(dir);
        var json = Path.Combine(dir, "asset.json");
        File.WriteAllText(json, DomainJson.Serialize(asset));
        return json;
    }

    public IReadOnlyList<LibraryAsset> List(string? category = null)
    {
        var root = category is null ? Root : Path.Combine(Root, category);
        if (!Directory.Exists(root))
            return [];
        return Directory.GetFiles(root, "asset.json", SearchOption.AllDirectories)
            .Select(path => DomainJson.Deserialize<LibraryAsset>(File.ReadAllText(path)))
            .ToArray();
    }
}
