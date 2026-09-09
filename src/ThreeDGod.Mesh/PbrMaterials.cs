using System.Numerics;
using SharpGLTF.Schema2;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Mesh;

public sealed class PbrPreset
{
    public string Name { get; init; } = "";
    public Vector4 BaseColor { get; init; }
    public float Metallic { get; init; }
    public float Roughness { get; init; }
}

public static class PbrMaterials
{
    public static IReadOnlyList<PbrPreset> Presets { get; } =
    [
        new() { Name = "Gold", BaseColor = new Vector4(0.83f, 0.69f, 0.22f, 1), Metallic = 1f, Roughness = 0.2f },
        new() { Name = "Silver", BaseColor = new Vector4(0.77f, 0.78f, 0.80f, 1), Metallic = 1f, Roughness = 0.18f },
        new() { Name = "Steel", BaseColor = new Vector4(0.56f, 0.57f, 0.58f, 1), Metallic = 0.9f, Roughness = 0.35f },
        new() { Name = "Leather", BaseColor = new Vector4(0.36f, 0.22f, 0.13f, 1), Metallic = 0f, Roughness = 0.7f },
        new() { Name = "Cloth", BaseColor = new Vector4(0.45f, 0.42f, 0.40f, 1), Metallic = 0f, Roughness = 0.85f },
        new() { Name = "Plastic", BaseColor = new Vector4(0.2f, 0.2f, 0.22f, 1), Metallic = 0f, Roughness = 0.4f },
        new() { Name = "Skin", BaseColor = new Vector4(0.82f, 0.64f, 0.52f, 1), Metallic = 0f, Roughness = 0.55f },
        new() { Name = "OrcSkin", BaseColor = new Vector4(0.28f, 0.42f, 0.22f, 1), Metallic = 0f, Roughness = 0.72f }
    ];

    public static PbrPreset Get(string name) =>
        Presets.First(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static PbrPreset FromPrompt(string prompt)
    {
        var p = prompt.ToLowerInvariant();
        var preset = Presets.FirstOrDefault(x => p.Contains(x.Name.ToLowerInvariant())) ?? Get("Silver");
        var roughness = preset.Roughness;
        if (p.Contains("weniger glänzend") || p.Contains("less shiny") || p.Contains("matte"))
            roughness = Math.Clamp(roughness + 0.25f, 0f, 1f);
        if (p.Contains("glänzender") || p.Contains("shinier"))
            roughness = Math.Clamp(roughness - 0.15f, 0f, 1f);
        return new PbrPreset { Name = preset.Name, BaseColor = preset.BaseColor, Metallic = preset.Metallic, Roughness = roughness };
    }

    public static PbrPreset FromHex(string name, string hexColor, float metallic, float roughness)
    {
        var rgba = ParseHex(hexColor);
        return new PbrPreset
        {
            Name = name,
            BaseColor = rgba,
            Metallic = Math.Clamp(metallic, 0f, 1f),
            Roughness = Math.Clamp(roughness, 0f, 1f)
        };
    }

    public static Vector4 ParseHex(string hex)
    {
        var h = hex.Trim().TrimStart('#');
        if (h.Length == 6)
            h += "FF";
        if (h.Length != 8)
            return new Vector4(0.8f, 0.8f, 0.8f, 1f);
        static byte Parse(string s) => Convert.ToByte(s, 16);
        return new Vector4(Parse(h[..2]) / 255f, Parse(h.Substring(2, 2)) / 255f, Parse(h.Substring(4, 2)) / 255f, Parse(h.Substring(6, 2)) / 255f);
    }

    public static MaterialDefinition ToDefinition(PbrPreset preset) =>
        new()
        {
            Name = preset.Name,
            BaseColorFactor = new ColorRgba { R = preset.BaseColor.X, G = preset.BaseColor.Y, B = preset.BaseColor.Z, A = preset.BaseColor.W },
            MetallicFactor = preset.Metallic,
            RoughnessFactor = preset.Roughness
        };

    public static void ApplyToGlb(string glbPath, PbrPreset preset)
    {
        var (positions, indices) = MeshCompare.ReadMesh(glbPath);
        TriangleMeshExport.WriteGlb(glbPath, positions, indices, preset.BaseColor, preset.Metallic, preset.Roughness);
    }

    public static (float Metallic, float Roughness, Vector4 Color) ReadFirst(string glbPath)
    {
        var model = ModelRoot.Load(glbPath);
        var material = model.LogicalMaterials[0];
        var color = material.FindChannel("BaseColor");
        var mr = material.FindChannel("MetallicRoughness");
        var rgba = color?.Color ?? Vector4.One;
        var metallic = 0f;
        var roughness = 1f;
        if (mr is { } channel)
        {
            metallic = channel.GetFactor("MetallicFactor");
            roughness = channel.GetFactor("RoughnessFactor");
        }
        return (metallic, roughness, new Vector4(rgba.X, rgba.Y, rgba.Z, rgba.W));
    }
}
