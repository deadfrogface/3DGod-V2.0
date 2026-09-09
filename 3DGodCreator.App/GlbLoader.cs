using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SharpGLTF.Schema2;
using ThreeDGodCreator.Core.Models;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

public static class GlbLoader
{
    public static Model3DGroup? Load(string path) => Load(path, materialOverride: null);

    public static Model3DGroup? Load(string path, MaterialData? materialOverride)
    {
        try
        {
            var model = ModelRoot.Load(path);
            if (model?.LogicalMeshes == null) return null;

            var group = new Model3DGroup();
            var rotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180));
            group.Transform = rotate;
            foreach (var mesh in model.LogicalMeshes)
            {
                foreach (var primitive in mesh.Primitives)
                {
                    var geo = BuildMeshGeometry(primitive);
                    if (geo != null)
                    {
                        var mat = materialOverride != null
                            ? CreateWpfMaterial(materialOverride)
                            : ReadWpfMaterial(primitive);
                        group.Children.Add(new GeometryModel3D(geo, mat));
                    }
                }
            }
            return group.Children.Count > 0 ? group : null;
        }
        catch (Exception ex)
        {
            AppLogger.Write($"[GlbLoader] Load failed: {path} - {ex.Message}", isError: true);
            AppLogger.LogException(ex, "GlbLoader.Load");
            DebugLog.Write($"[GlbLoader] Fehler: {ex.Message}");
            return null;
        }
    }

    public static void ApplyMaterialOverride(Model3D model, MaterialData data)
    {
        if (model is GeometryModel3D gm)
            gm.Material = CreateWpfMaterial(data);
        else if (model is Model3DGroup grp)
        {
            foreach (Model3D child in grp.Children)
                ApplyMaterialOverride(child, data);
        }
    }

    public static System.Windows.Media.Media3D.Material CreateWpfMaterial(MaterialData data)
    {
        var rgba = ParseHexColor(data.Color);
        return CreateWpfMaterial(rgba, (float)data.Metallic, (float)data.Roughness);
    }

    public static System.Windows.Media.Media3D.Material CreateWpfMaterial(Vector4 baseColor, float metallic, float roughness)
    {
        var brush = new SolidColorBrush(Color.FromArgb(
            (byte)Math.Clamp(baseColor.W * 255f, 0, 255),
            (byte)Math.Clamp(baseColor.X * 255f, 0, 255),
            (byte)Math.Clamp(baseColor.Y * 255f, 0, 255),
            (byte)Math.Clamp(baseColor.Z * 255f, 0, 255)));
        brush.Freeze();

        var specPower = Math.Clamp((1f - roughness) * (8f + metallic * 40f), 1f, 64f);
        var specIntensity = Math.Clamp((1f - roughness) * (0.15f + metallic * 0.65f), 0f, 1f);
        if (specIntensity <= 0.01f)
        {
            var diffuseOnly = new DiffuseMaterial(brush);
            diffuseOnly.Freeze();
            return diffuseOnly;
        }

        var specBrush = new SolidColorBrush(Color.FromScRgb(specIntensity, 1f, 1f, 1f));
        specBrush.Freeze();
        var group = new MaterialGroup
        {
            Children = { new DiffuseMaterial(brush), new SpecularMaterial(specBrush, specPower) }
        };
        group.Freeze();
        return group;
    }

    private static System.Windows.Media.Media3D.Material ReadWpfMaterial(MeshPrimitive primitive)
    {
        var color = Vector4.One;
        var metallic = 0f;
        var roughness = 0.5f;
        var gltfMat = primitive.Material;
        if (gltfMat != null)
        {
            var bc = gltfMat.FindChannel("BaseColor");
            if (bc?.Color is Vector4 rgba)
                color = rgba;
            var mr = gltfMat.FindChannel("MetallicRoughness");
            if (mr is { } channel)
            {
                metallic = channel.GetFactor("MetallicFactor");
                roughness = channel.GetFactor("RoughnessFactor");
            }
        }
        return CreateWpfMaterial(color, metallic, roughness);
    }

    private static Vector4 ParseHexColor(string hex)
    {
        var h = hex.Trim().TrimStart('#');
        if (h.Length == 6)
            h += "FF";
        if (h.Length != 8)
            return new Vector4(0.8f, 0.8f, 0.8f, 1f);
        static byte Parse(string s) => Convert.ToByte(s, 16);
        return new Vector4(Parse(h[..2]) / 255f, Parse(h.Substring(2, 2)) / 255f, Parse(h.Substring(4, 2)) / 255f, Parse(h.Substring(6, 2)) / 255f);
    }

    private static MeshGeometry3D? BuildMeshGeometry(MeshPrimitive primitive)
    {
        var posAccessor = primitive.GetVertexAccessor("POSITION");
        if (posAccessor == null) return null;

        var positions = new Point3DCollection();
        foreach (var v in posAccessor.AsVector3Array())
            positions.Add(new Point3D(v.X, v.Y, v.Z));

        var indices = new Int32Collection();
        var indexAccessor = primitive.GetIndexAccessor();
        if (indexAccessor != null)
        {
            foreach (var i in indexAccessor.AsIndicesArray())
                indices.Add((int)i);
        }
        else
        {
            for (int i = 0; i < positions.Count; i++)
                indices.Add(i);
        }

        var normAccessor = primitive.GetVertexAccessor("NORMAL");
        var normals = new Vector3DCollection();
        if (normAccessor != null)
        {
            foreach (var n in normAccessor.AsVector3Array())
                normals.Add(new Vector3D(n.X, n.Y, n.Z));
        }

        var geo = new MeshGeometry3D
        {
            Positions = positions,
            TriangleIndices = indices
        };
        if (normals.Count == positions.Count)
            geo.Normals = normals;

        return geo;
    }
}
