using System.IO.Compression;
using ThreeDGod.Mesh;

namespace ThreeDGod.Infrastructure;

public sealed record Cc0AssetPack(
    string Id,
    Uri DownloadUri,
    string License,
    string SourcePage,
    string Topology,
    string[] KnownAssets);

public sealed class Cc0CreatureAssetPackService
{
    public static readonly Cc0AssetPack Bodyparts01 = new(
        "makehuman-bodyparts01",
        new Uri("https://files2.makehumancommunity.org/asset_packs/bodyparts01/bodyparts01_cc0.zip"),
        "CC0-1.0",
        "https://static.makehumancommunity.org/assets/assetpacks/bodyparts01.html",
        "mhclo/hm08",
        ["culturalibre_faun_horns", "culturalibre_minotaur_horns", "freezychan_lucoa_quetzalcoatl_horns", "jaldmic_houndoom_horns"]);

    public static readonly Cc0AssetPack Animal01 = new(
        "makehuman-animal01",
        new Uri("https://files2.makehumancommunity.org/asset_packs/animal01/animal01_cc0.zip"),
        "CC0-1.0",
        "https://static.makehumancommunity.org/assets/assetpacks/animal01.html",
        "hm08-target",
        ["culturalibre_faun_face", "elvs_piggy_nose1", "jaldmic_donkey_head", "jaldmic_equinus_headv2", "titleknown_catgirl_ears"]);

    public static readonly Cc0AssetPack Equipment01 = new(
        "makehuman-equipment01",
        new Uri("https://files2.makehumancommunity.org/asset_packs/equipment01/equipment01_cc0.zip"),
        "CC0-1.0",
        "https://static.makehumancommunity.org/assets/assetpacks/equipment01.html",
        "mhclo/hm08",
        ["culturalibre_war_hammer", "culturalibre_wooden_bow", "joepal_crude_sword", "o4saken_dagger"]);

    public static readonly Cc0AssetPack Hair01 = new(
        "makehuman-hair01",
        new Uri("https://files2.makehumancommunity.org/asset_packs/hair01/hair01_cc0.zip"),
        "CC0-1.0",
        "https://static.makehumancommunity.org/assets/assetpacks/hair01.html",
        "mhclo/hm08",
        ["cortu_short_messy_hair", "culturalibre_hair_01", "learning_anime_hair", "toigo_blunt_bob"]);

    public static readonly Cc0AssetPack Shirts01 = new(
        "makehuman-shirts01",
        new Uri("https://files2.makehumancommunity.org/asset_packs/shirts01/shirts01_cc0.zip"),
        "CC0-1.0",
        "https://static.makehumancommunity.org/assets/assetpacks/shirts01.html",
        "mhclo/hm08",
        ["elvs_crude_t-shirt_male", "joepal_crude_t-shirt_female", "namuhekam_male_polo_shirt", "toigo_fisherman_sweater"]);

    public static readonly Cc0AssetPack Suits02 = new(
        "makehuman-suits02",
        new Uri("https://files2.makehumancommunity.org/asset_packs/suits02/suits02_cc0.zip"),
        "CC0-1.0",
        "https://static.makehumancommunity.org/assets/assetpacks/suits02.html",
        "mhclo/hm08",
        ["matcreator_mc-scifi-armor_guardian", "matcreator_mc-scifi-armor_helios", "matcreator_mc-scifi-armor_jupiter7", "rehmanpolanski_viking_tunic"]);

    private readonly HttpClient _http;
    private readonly string _root;

    public Cc0CreatureAssetPackService(HttpClient? http = null, string? root = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        _root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "3DGod", "Assets", "CC0");
    }

    public string Root => _root;

    public bool IsInstalled(Cc0AssetPack pack) =>
        File.Exists(Path.Combine(_root, pack.Id, ".installed"));

    public async Task<string> InstallAsync(Cc0AssetPack pack, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(pack.License, "CC0-1.0", StringComparison.Ordinal))
            throw new InvalidOperationException("Only verified CC0 packs are accepted by this installer.");

        var destination = Path.Combine(_root, pack.Id);
        Directory.CreateDirectory(destination);
        var zipPath = Path.Combine(destination, "pack.zip");

        using (var response = await _http.GetAsync(pack.DownloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = File.Create(zipPath);
            await source.CopyToAsync(output, cancellationToken);
        }

        using (var zip = ZipFile.OpenRead(zipPath))
        {
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                var rootFull = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
                if (!target.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("CC0 asset pack contained an unsafe archive path.");
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: true);
            }
        }

        File.Delete(zipPath);
        File.WriteAllText(Path.Combine(destination, ".installed"),
            $"id={pack.Id}\nlicense={pack.License}\nsource={pack.SourcePage}\ntopology={pack.Topology}\ninstalledUtc={DateTime.UtcNow:O}\n");
        return destination;
    }

    public IReadOnlyList<string> ListAnimalTargets()
    {
        var root = Path.Combine(_root, Animal01.Id);
        if (!Directory.Exists(root)) return [];
        return Directory.EnumerateFiles(root, "*.target", SearchOption.AllDirectories)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string? FindAnimalTarget(params string[] nameFragments)
    {
        var targets = ListAnimalTargets();
        return targets.FirstOrDefault(path =>
            nameFragments.All(fragment =>
                Path.GetFileNameWithoutExtension(path).Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    public IReadOnlyList<string> ListMeshObjs(params Cc0AssetPack[] packs)
    {
        var result = new List<string>();
        foreach (var pack in packs)
        {
            var root = Path.Combine(_root, pack.Id);
            if (!Directory.Exists(root)) continue;
            result.AddRange(Directory.EnumerateFiles(root, "*.obj", SearchOption.AllDirectories));
        }
        return result.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public string? FindObj(string assetName)
    {
        if (!Directory.Exists(_root)) return null;
        return Directory.EnumerateFiles(_root, "*.obj", SearchOption.AllDirectories)
            .FirstOrDefault(p => string.Equals(Path.GetFileNameWithoutExtension(p), assetName, StringComparison.OrdinalIgnoreCase));
    }

    public string ConvertObjAssetToGlb(string assetName, string destinationGlb)
    {
        var obj = File.Exists(assetName) ? assetName : FindObj(assetName)
            ?? throw new FileNotFoundException($"CC0 asset '{assetName}' is not installed.");
        return TriangleMeshExport.ObjToGlb(obj, destinationGlb);
    }

    public static void AssertTargetCompatibleWithTopology(Cc0AssetPack pack, string targetTopology)
    {
        if (pack.Topology.Contains("hm08", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(targetTopology, "hm08", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(targetTopology, "makehuman", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Asset pack '{pack.Id}' contains hm08 topology-bound morph data and cannot be applied directly to '{targetTopology}'. Retarget/bake is required.");
    }
}
