using System.Numerics;
using g3;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Mesh;

/// <summary>
/// geometry3Sharp proximity / winding queries for garment-on-body fit.
/// Projection + inflate only — not cloth simulation.
/// </summary>
public static class GarmentProximityFitter
{
    public const float DefaultInflateMeters = 0.008f;

    public static BodyMeasurements Measure(IReadOnlyList<Vector3> body, string sourceGlb = "")
    {
        if (body.Count < 8)
            throw new InvalidOperationException("Body mesh has too few vertices to measure.");
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
        foreach (var p in body)
        {
            minX = MathF.Min(minX, p.X); maxX = MathF.Max(maxX, p.X);
            minY = MathF.Min(minY, p.Y); maxY = MathF.Max(maxY, p.Y);
            minZ = MathF.Min(minZ, p.Z); maxZ = MathF.Max(maxZ, p.Z);
        }
        var height = maxY - minY;
        if (height < 0.05f)
            throw new InvalidOperationException("Body height is degenerate.");

        var chest = SpanInBand(body, minY + height * 0.48f, minY + height * 0.68f);
        var waist = SpanInBand(body, minY + height * 0.32f, minY + height * 0.48f);
        var shoulder = SpanInBand(body, minY + height * 0.68f, minY + height * 0.82f);
        var torso = CentroidInBand(body, minY + height * 0.45f, minY + height * 0.72f);

        return new BodyMeasurements
        {
            SourceGlb = sourceGlb,
            HeightM = height,
            MinY = minY,
            MaxY = maxY,
            ChestWidthM = chest,
            WaistWidthM = waist,
            ShoulderWidthM = shoulder,
            TorsoCenterX = torso.X,
            TorsoCenterY = torso.Y,
            TorsoCenterZ = torso.Z
        };
    }

    public static (List<Vector3> Fitted, ClippingReport Report) Fit(
        IReadOnlyList<Vector3> bodyPositions,
        IReadOnlyList<int> bodyIndices,
        IReadOnlyList<Vector3> garmentPositions,
        IReadOnlyList<int> garmentIndices,
        BodyMeasurements measurements,
        float inflateMeters = DefaultInflateMeters)
    {
        if (garmentPositions.Count < 3)
            throw new InvalidOperationException("Garment mesh has no vertices.");
        _ = garmentIndices;

        var body = ToDMesh(bodyPositions, bodyIndices);
        var tree = new DMeshAABBTree3(body, true);
        var torso = new Vector3(measurements.TorsoCenterX, measurements.TorsoCenterY, measurements.TorsoCenterZ);
        var garmentCentroid = GeometryQueryService.Centroid(garmentPositions);
        var offset = torso - garmentCentroid;

        var placed = garmentPositions.Select(p => p + offset).ToList();
        var insideBefore = CountInside(tree, placed);
        var fitted = placed;
        for (var pass = 0; pass < 4; pass++)
            fitted = ProjectInflate(tree, body, fitted, inflateMeters);
        var insideAfter = CountInside(tree, fitted);
        var minAfter = MinDistance(tree, body, fitted);
        var minY = measurements.MinY;
        var height = measurements.HeightM;

        return (fitted, new ClippingReport
        {
            Measurements = measurements,
            SampleCount = fitted.Count,
            InsideBefore = insideBefore,
            InsideAfter = insideAfter,
            MinDistanceAfter = minAfter,
            InflateMeters = inflateMeters,
            Backend = "geometry3Sharp",
            Regions =
            [
                Region(tree, fitted, "legs", minY, minY + height * 0.35f),
                Region(tree, fitted, "torso", minY + height * 0.35f, minY + height * 0.75f),
                Region(tree, fitted, "head", minY + height * 0.75f, minY + height * 1.2f)
            ]
        });
    }

    private static List<Vector3> ProjectInflate(DMeshAABBTree3 tree, DMesh3 body, List<Vector3> verts, float inflate)
    {
        var result = new List<Vector3>(verts.Count);
        for (var i = 0; i < verts.Count; i++)
        {
            var p = verts[i];
            var gd = new Vector3d(p.X, p.Y, p.Z);
            var tid = tree.FindNearestTriangle(gd, out var _, double.MaxValue);
            if (tid < 0)
            {
                result.Add(p);
                continue;
            }
            var dist = MeshQueries.TriangleDistance(body, tid, gd);
            var closest = dist.TriangleClosest;
            var n = body.GetTriNormal(tid);
            if (n.LengthSquared < 1e-12)
            {
                result.Add(p);
                continue;
            }
            n.Normalize();
            if (tree.IsInside(gd))
            {
                var q = closest + n * inflate;
                result.Add(new Vector3((float)q.x, (float)q.y, (float)q.z));
            }
            else
                result.Add(p);
        }
        return result;
    }

    private static int CountInside(DMeshAABBTree3 tree, IReadOnlyList<Vector3> verts)
    {
        var n = 0;
        foreach (var p in verts)
        {
            if (tree.IsInside(new Vector3d(p.X, p.Y, p.Z)))
                n++;
        }
        return n;
    }

    private static float MinDistance(DMeshAABBTree3 tree, DMesh3 body, IReadOnlyList<Vector3> verts)
    {
        var min = float.MaxValue;
        foreach (var p in verts)
        {
            var gd = new Vector3d(p.X, p.Y, p.Z);
            var tid = tree.FindNearestTriangle(gd, out var distSqr, double.MaxValue);
            if (tid < 0) continue;
            var d = MathF.Sqrt((float)distSqr);
            if (tree.IsInside(gd))
                d = -d;
            min = MathF.Min(min, d);
        }
        return min == float.MaxValue ? 0 : min;
    }

    private static ClippingRegion Region(DMeshAABBTree3 tree, IReadOnlyList<Vector3> verts, string name, float y0, float y1)
    {
        var inside = 0;
        var min = float.MaxValue;
        var any = false;
        foreach (var p in verts)
        {
            if (p.Y < y0 || p.Y > y1) continue;
            any = true;
            var gd = new Vector3d(p.X, p.Y, p.Z);
            if (tree.IsInside(gd)) inside++;
            var tid = tree.FindNearestTriangle(gd, out var distSqr, double.MaxValue);
            if (tid >= 0)
                min = MathF.Min(min, MathF.Sqrt((float)distSqr));
        }
        return new ClippingRegion
        {
            Name = name,
            InsideCount = inside,
            MinDistance = any && min != float.MaxValue ? min : 0
        };
    }

    private static DMesh3 ToDMesh(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices)
    {
        var mesh = new DMesh3();
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            mesh.AppendVertex(new Vector3d(p.X, p.Y, p.Z));
        }
        for (var i = 0; i + 2 < indices.Count; i += 3)
        {
            var a = indices[i];
            var b = indices[i + 1];
            var c = indices[i + 2];
            if (a == b || b == c || a == c) continue;
            if (a < 0 || b < 0 || c < 0 || a >= positions.Count || b >= positions.Count || c >= positions.Count)
                continue;
            mesh.AppendTriangle(a, b, c);
        }
        if (mesh.TriangleCount < 1)
            throw new InvalidOperationException("geometry3Sharp mesh has no triangles.");
        return mesh;
    }

    private static float SpanInBand(IReadOnlyList<Vector3> body, float y0, float y1)
    {
        var min = float.MaxValue;
        var max = float.MinValue;
        var any = false;
        foreach (var p in body)
        {
            if (p.Y < y0 || p.Y > y1) continue;
            any = true;
            min = MathF.Min(min, p.X);
            max = MathF.Max(max, p.X);
        }
        if (!any) return 0.3f;
        var xSpan = max - min;
        min = float.MaxValue; max = float.MinValue;
        foreach (var p in body)
        {
            if (p.Y < y0 || p.Y > y1) continue;
            min = MathF.Min(min, p.Z);
            max = MathF.Max(max, p.Z);
        }
        return MathF.Max(xSpan, max - min);
    }

    private static Vector3 CentroidInBand(IReadOnlyList<Vector3> body, float y0, float y1)
    {
        var sum = Vector3.Zero;
        var n = 0;
        foreach (var p in body)
        {
            if (p.Y < y0 || p.Y > y1) continue;
            sum += p;
            n++;
        }
        return n == 0 ? GeometryQueryService.Centroid(body) : sum / n;
    }
}
