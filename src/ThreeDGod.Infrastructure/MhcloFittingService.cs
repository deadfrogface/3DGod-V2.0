using System.Globalization;
using System.Numerics;
using ThreeDGod.Mesh;

namespace ThreeDGod.Infrastructure;

/// <summary>
/// Applies MakeHuman MHCLO proxy mappings to an hm08/makehuman body without invoking AGPL code.
/// Supports direct vertex mappings, 3-vertex barycentric mapping rows and x/y/z reference scaling.
/// Coordinates stay in the MakeHuman/Anny y-up space; Blender-specific Y/Z conversion is intentionally not applied.
/// Unsupported vertex rows fail closed rather than silently producing a badly fitted asset.
/// </summary>
public sealed class MhcloFittingService
{
    public MhcloFitResult Fit(string bodyGlb, string assetObj, string mhcloPath, string destinationGlb)
    {
        if (!File.Exists(bodyGlb)) throw new FileNotFoundException("MakeHuman body GLB not found.", bodyGlb);
        if (!File.Exists(assetObj)) throw new FileNotFoundException("MHCLO asset OBJ not found.", assetObj);
        if (!File.Exists(mhcloPath)) throw new FileNotFoundException("MHCLO mapping not found.", mhcloPath);

        var (body, _) = MeshCompare.ReadMesh(bodyGlb);
        var asset = ObjImporter.ImportObj(assetObj);
        var mappings = Parse(mhcloPath);
        var scale = ComputeScale(mhcloPath, body);
        if (mappings.Count != asset.Positions.Count)
            throw new InvalidDataException($"MHCLO mapping count {mappings.Count} does not match OBJ vertex count {asset.Positions.Count}.");

        var fitted = new List<Vector3>(mappings.Count);
        foreach (var m in mappings)
        {
            Vector3 p;
            if (m.DirectVertex is int direct)
            {
                RequireIndex(direct, body.Count);
                p = body[direct] + Vector3.Multiply(m.Offset, scale);
            }
            else
            {
                RequireIndex(m.V0, body.Count); RequireIndex(m.V1, body.Count); RequireIndex(m.V2, body.Count);
                p = body[m.V0] * m.W0 + body[m.V1] * m.W1 + body[m.V2] * m.W2 + Vector3.Multiply(m.Offset, scale);
            }
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z))
                throw new InvalidDataException("MHCLO fitting produced a non-finite vertex.");
            fitted.Add(p);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationGlb))!);
        TriangleMeshExport.WriteGlb(destinationGlb, fitted, asset.Indices);
        var doc = CanonicalGltfPipeline.Load(destinationGlb);
        if (doc.VertexCount < 3 || doc.TriangleCount < 1)
            throw new InvalidDataException("Fitted MHCLO output is not a usable mesh.");
        return new MhcloFitResult(destinationGlb, fitted.Count, asset.Indices.Count / 3, mappings.Count);
    }

    public static string? FindMhcloForObj(string objPath)
    {
        var dir = Path.GetDirectoryName(objPath);
        if (dir is null) return null;
        var stem = Path.GetFileNameWithoutExtension(objPath);
        var exact = Path.Combine(dir, stem + ".mhclo");
        if (File.Exists(exact)) return exact;
        return Directory.EnumerateFiles(dir, "*.mhclo", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(p => string.Equals(Path.GetFileNameWithoutExtension(p), stem, StringComparison.OrdinalIgnoreCase))
            ?? Directory.EnumerateFiles(dir, "*.mhclo", SearchOption.TopDirectoryOnly).FirstOrDefault();
    }

    internal static Vector3 ComputeScale(string path, IReadOnlyList<Vector3> body)
    {
        var result = Vector3.One;
        foreach (var raw in File.ReadLines(path))
        {
            var p = raw.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 4 || p[0] is not ("x_scale" or "y_scale" or "z_scale")) continue;
            if (!int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var a)
                || !int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
                throw new InvalidDataException("Malformed MHCLO scale vertex indices.");
            RequireIndex(a, body.Count); RequireIndex(b, body.Count);
            var reference = float.Parse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture);
            if (!float.IsFinite(reference) || MathF.Abs(reference) < 1e-6f)
                throw new InvalidDataException("Malformed MHCLO reference scale.");
            var axisDistance = p[0] switch
            {
                "x_scale" => MathF.Abs(body[a].X - body[b].X),
                "y_scale" => MathF.Abs(body[a].Y - body[b].Y),
                _ => MathF.Abs(body[a].Z - body[b].Z)
            };
            var factor = axisDistance / MathF.Abs(reference);
            if (!float.IsFinite(factor) || factor <= 0) factor = 1f;
            if (p[0] == "x_scale") result.X = factor;
            else if (p[0] == "y_scale") result.Y = factor;
            else result.Z = factor;
        }
        return result;
    }

    internal static IReadOnlyList<MhcloVertexMap> Parse(string path)
    {
        var maps = new List<MhcloVertexMap>();
        var inVerts = false;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith("verts", StringComparison.OrdinalIgnoreCase)) { inVerts = true; continue; }
            if (!inVerts) continue;
            if (char.IsLetter(line[0]))
            {
                var key = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
                // Some real MakeHuman system assets contain metadata (notably material)
                // after "verts 0" and then continue the vertex mapping section.
                if (key is "material" or "obj_file" or "z_depth" or "max_pole" or "tag")
                    continue;
                if (key is "weights" or "delete_verts")
                    break;
                throw new InvalidDataException("Unsupported directive inside MHCLO vertex mapping: " + key);
            }

            var p = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 1 && int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var direct))
            {
                maps.Add(MhcloVertexMap.Direct(direct));
                continue;
            }
            if (p.Length >= 9
                && int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v0)
                && int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v1)
                && int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v2))
            {
                var w0 = F(p[3]); var w1 = F(p[4]); var w2 = F(p[5]);
                var sum = w0 + w1 + w2;
                if (!float.IsFinite(sum) || MathF.Abs(sum) < 1e-6f)
                    throw new InvalidDataException("MHCLO barycentric weights are invalid.");
                // Normalize defensively; real files are expected to sum to ~1.
                w0 /= sum; w1 /= sum; w2 /= sum;
                maps.Add(MhcloVertexMap.Bary(v0, v1, v2, w0, w1, w2, new Vector3(F(p[6]), F(p[7]), F(p[8]))));
                continue;
            }
            throw new InvalidDataException("Unsupported or malformed MHCLO vertex mapping: " + line);
        }
        if (maps.Count == 0) throw new InvalidDataException("MHCLO file contains no vertex mappings.");
        return maps;

        static float F(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static void RequireIndex(int index, int count)
    {
        if ((uint)index >= (uint)count)
            throw new InvalidDataException($"MHCLO body vertex index {index} is outside body vertex count {count}.");
    }
}

public sealed record MhcloFitResult(string FittedGlb, int VertexCount, int TriangleCount, int MappingCount);

internal sealed record MhcloVertexMap(int? DirectVertex, int V0, int V1, int V2, float W0, float W1, float W2, Vector3 Offset)
{
    public static MhcloVertexMap Direct(int v) => new(v, 0, 0, 0, 0, 0, 0, Vector3.Zero);
    public static MhcloVertexMap Bary(int a, int b, int c, float wa, float wb, float wc, Vector3 offset) =>
        new(null, a, b, c, wa, wb, wc, offset);
}
