using System.Numerics;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Mesh;

public sealed class RemeshMesh
{
    public required IReadOnlyList<Vector3> Positions { get; init; }
    public required IReadOnlyList<int> Indices { get; init; }
    public required IReadOnlyList<Vector2> Uvs { get; init; }
    public RemeshProfile Profile { get; init; }
    public string BackendId { get; init; } = "vertex-cluster";
}

/// <summary>
/// In-process remesher modeled on meshoptimizer's sloppy simplify (grid clustering)
/// plus a spherical UV unwrap. Native instant-meshes / xatlas binaries are not shipped.
/// This is real geometry reduction, not triangle-count integer division.
/// </summary>
public static class RemeshPipeline
{
    public static float TargetRatio(RemeshProfile profile) => profile switch
    {
        RemeshProfile.KeepOriginal => 1f,
        RemeshProfile.CharacterCandidate => 0.55f,
        RemeshProfile.StaticGameAsset => 0.28f,
        RemeshProfile.Preview => 0.12f,
        _ => 1f
    };

    public static RemeshMesh Run(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices, RemeshProfile profile)
    {
        if (positions.Count < 3 || indices.Count < 3)
            throw new InvalidOperationException("Remesh input has no triangles.");

        var source = profile == RemeshProfile.KeepOriginal
            ? (Positions: positions.ToList(), Indices: indices.ToList())
            : Weld(positions, indices);
        var reduced = profile == RemeshProfile.KeepOriginal
            ? source
            : ClusterSimplify(source.Positions, source.Indices, TargetRatio(profile));

        var uvs = UvUnwrapper.Spherical(reduced.Positions);
        var report = MeshValidator.Validate(reduced.Positions, reduced.Indices, uvCount: uvs.Count);
        if (report.Rejected)
            throw new InvalidOperationException(
                "Remesh produced a rejected mesh: " + string.Join("; ", report.Issues.Select(i => i.Code)));

        return new RemeshMesh
        {
            Positions = reduced.Positions,
            Indices = reduced.Indices,
            Uvs = uvs,
            Profile = profile,
            BackendId = "vertex-cluster"
        };
    }

    public static string RemeshGlb(string sourceGlb, string destinationGlb, RemeshProfile profile)
    {
        var (positions, indices) = MeshCompare.ReadMesh(sourceGlb);
        var result = Run(positions, indices, profile);
        TriangleMeshExport.WriteGlb(destinationGlb, result.Positions, result.Indices, result.Uvs);
        return destinationGlb;
    }

    public static (List<Vector3> Positions, List<int> Indices) Weld(
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<int> indices,
        float epsilon = 1e-5f)
    {
        var map = new Dictionary<(int X, int Y, int Z), int>();
        var welded = new List<Vector3>();
        var remap = new int[positions.Count];
        var inv = 1f / MathF.Max(epsilon, 1e-8f);
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            var key = ((int)MathF.Round(p.X * inv), (int)MathF.Round(p.Y * inv), (int)MathF.Round(p.Z * inv));
            if (!map.TryGetValue(key, out var index))
            {
                index = welded.Count;
                map[key] = index;
                welded.Add(p);
            }
            remap[i] = index;
        }

        var outIdx = new List<int>(indices.Count);
        for (var i = 0; i + 2 < indices.Count; i += 3)
        {
            var a = remap[indices[i]];
            var b = remap[indices[i + 1]];
            var c = remap[indices[i + 2]];
            if (a == b || b == c || a == c)
                continue;
            outIdx.Add(a);
            outIdx.Add(b);
            outIdx.Add(c);
        }

        if (outIdx.Count < 3)
            throw new InvalidOperationException("Weld removed every triangle.");
        return (welded, outIdx);
    }

    public static (List<Vector3> Positions, List<int> Indices) ClusterSimplify(
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<int> indices,
        float ratio)
    {
        var sourceTris = indices.Count / 3;
        var targetTris = Math.Max(8, (int)MathF.Ceiling(sourceTris * Math.Clamp(ratio, 0.02f, 0.95f)));
        if (sourceTris <= targetTris)
            return (positions.ToList(), indices.ToList());

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in positions)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        var extent = MathF.Max(1e-4f, MathF.Max(max.X - min.X, MathF.Max(max.Y - min.Y, max.Z - min.Z)));

        var cells = EstimateCells(positions.Count, ratio);
        (List<Vector3> Positions, List<int> Indices)? best = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var clustered = ClusterOnce(positions, indices, min, extent, cells);
            if (clustered.Indices.Count < 3)
            {
                cells = Math.Max(2, (int)(cells * 1.4f));
                continue;
            }

            var tris = clustered.Indices.Count / 3;
            best = clustered;
            if (tris <= targetTris)
                break;
            cells = Math.Max(2, (int)(cells / 1.35f));
        }

        if (best is null)
            throw new InvalidOperationException("Vertex clustering produced an empty mesh.");
        return best.Value;
    }

    private static int EstimateCells(int vertexCount, float ratio)
    {
        var targetVerts = Math.Max(12, (int)MathF.Ceiling(vertexCount * MathF.Sqrt(ratio)));
        return Math.Clamp((int)MathF.Ceiling(MathF.Cbrt(targetVerts)), 3, 96);
    }

    private static (List<Vector3> Positions, List<int> Indices) ClusterOnce(
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<int> indices,
        Vector3 min,
        float extent,
        int cells)
    {
        var cellSize = extent / cells;
        var sums = new Dictionary<(int X, int Y, int Z), (Vector3 Sum, int Count)>();
        var remap = new int[positions.Count];
        var keys = new (int X, int Y, int Z)[positions.Count];
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            var key = (
                ClampCell((int)MathF.Floor((p.X - min.X) / cellSize), cells),
                ClampCell((int)MathF.Floor((p.Y - min.Y) / cellSize), cells),
                ClampCell((int)MathF.Floor((p.Z - min.Z) / cellSize), cells));
            keys[i] = key;
            if (sums.TryGetValue(key, out var acc))
                sums[key] = (acc.Sum + p, acc.Count + 1);
            else
                sums[key] = (p, 1);
        }

        var compact = new Dictionary<(int X, int Y, int Z), int>(sums.Count);
        var outPos = new List<Vector3>(sums.Count);
        foreach (var (key, acc) in sums)
        {
            compact[key] = outPos.Count;
            outPos.Add(acc.Sum / acc.Count);
        }
        for (var i = 0; i < positions.Count; i++)
            remap[i] = compact[keys[i]];

        var outIdx = new List<int>();
        for (var i = 0; i + 2 < indices.Count; i += 3)
        {
            var a = remap[indices[i]];
            var b = remap[indices[i + 1]];
            var c = remap[indices[i + 2]];
            if (a < 0 || b < 0 || c < 0 || a >= outPos.Count || b >= outPos.Count || c >= outPos.Count)
                throw new InvalidOperationException("Clustering produced an invalid index.");
            if (a == b || b == c || a == c)
                continue;
            outIdx.Add(a);
            outIdx.Add(b);
            outIdx.Add(c);
        }

        return (outPos, outIdx);
    }

    private static int ClampCell(int value, int cells) => Math.Clamp(value, 0, cells - 1);
}

public static class UvUnwrapper
{
    public static IReadOnlyList<Vector2> Spherical(IReadOnlyList<Vector3> positions)
    {
        var centroid = GeometryQueryService.Centroid(positions);
        var uvs = new Vector2[positions.Count];
        for (var i = 0; i < positions.Count; i++)
        {
            var d = positions[i] - centroid;
            var len = d.Length();
            if (len < 1e-8f)
            {
                uvs[i] = new Vector2(0.5f, 0.5f);
                continue;
            }
            d /= len;
            var u = 0.5f + MathF.Atan2(d.Z, d.X) / (MathF.PI * 2f);
            var v = 0.5f + MathF.Asin(Math.Clamp(d.Y, -1f, 1f)) / MathF.PI;
            uvs[i] = new Vector2(Wrap01(u), Math.Clamp(v, 0f, 1f));
        }
        return uvs;
    }

    private static float Wrap01(float u)
    {
        u %= 1f;
        return u < 0 ? u + 1f : u;
    }
}
