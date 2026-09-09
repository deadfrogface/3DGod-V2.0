using System.Numerics;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Mesh;

public enum MeshIssueSeverity
{
    Hard,
    Soft
}

public sealed class MeshIssue
{
    public MeshIssueSeverity Severity { get; init; }
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed class MeshValidationReport
{
    public bool Rejected => Issues.Any(i => i.Severity == MeshIssueSeverity.Hard);
    public List<MeshIssue> Issues { get; } = [];
}

public static class MeshValidator
{
    public const int HugeTriangleThreshold = 2_000_000;

    public static MeshValidationReport Validate(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices, int uvCount, bool manifoldHint = true, int connectedComponents = 1)
    {
        var report = new MeshValidationReport();
        if (positions.Any(p => float.IsNaN(p.X) || float.IsNaN(p.Y) || float.IsNaN(p.Z)))
            report.Issues.Add(new MeshIssue { Severity = MeshIssueSeverity.Hard, Code = "NaN", Message = "Positions contain NaN." });
        for (var i = 0; i < indices.Count; i++)
        {
            if (indices[i] < 0 || indices[i] >= positions.Count)
            {
                report.Issues.Add(new MeshIssue { Severity = MeshIssueSeverity.Hard, Code = "InvalidIndex", Message = $"Index {indices[i]} out of range." });
                break;
            }
        }
        if (uvCount <= 0)
            report.Issues.Add(new MeshIssue { Severity = MeshIssueSeverity.Soft, Code = "NoUv", Message = "Mesh has no UV set." });
        if (!manifoldHint)
            report.Issues.Add(new MeshIssue { Severity = MeshIssueSeverity.Soft, Code = "NonManifold", Message = "Non-manifold geometry." });
        if (indices.Count / 3 >= HugeTriangleThreshold)
            report.Issues.Add(new MeshIssue { Severity = MeshIssueSeverity.Hard, Code = "HugeMesh", Message = "Triangle count exceeds hard limit." });
        if (connectedComponents > 1)
            report.Issues.Add(new MeshIssue { Severity = MeshIssueSeverity.Soft, Code = "Disconnected", Message = "Mesh has disconnected components." });
        return report;
    }
}

public static class GeometryQueryService
{
    public static Vector3 Centroid(IReadOnlyList<Vector3> positions) =>
        positions.Count == 0 ? Vector3.Zero : positions.Aggregate(Vector3.Zero, (a, b) => a + b) / positions.Count;
}

public static class LodService
{
    /// <summary>
    /// Triangle-budget arithmetic only. Does NOT generate a simplified mesh.
    /// Use <see cref="BuildLodMesh"/> for real geometry reduction via RemeshPipeline.
    /// </summary>
    public static int EstimateTriangleBudget(int sourceTriangles, int level) =>
        Math.Max(1, sourceTriangles / (int)Math.Pow(2, Math.Max(0, level)));

    /// <summary>Obsolete alias kept for callers; not real LOD generation.</summary>
    public static int TriangleCountForLod(int sourceTriangles, int level) =>
        EstimateTriangleBudget(sourceTriangles, level);

    /// <summary>
    /// Builds a real reduced mesh for the given LOD level using vertex-cluster remesh (not integer division).
    /// </summary>
    public static RemeshMesh BuildLodMesh(
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<int> indices,
        int level)
    {
        var profile = level switch
        {
            <= 0 => RemeshProfile.KeepOriginal,
            1 => RemeshProfile.CharacterCandidate,
            2 => RemeshProfile.StaticGameAsset,
            _ => RemeshProfile.Preview
        };
        var mesh = RemeshPipeline.Run(positions, indices, profile);
        return new RemeshMesh
        {
            Positions = mesh.Positions,
            Indices = mesh.Indices,
            Uvs = mesh.Uvs,
            Profile = mesh.Profile,
            BackendId = "lod-vertex-cluster"
        };
    }
}
