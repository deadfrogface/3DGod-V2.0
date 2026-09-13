using System.Numerics;
using g3;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Mesh;

/// <summary>
/// Adapter boundary for remesh / simplify backends.
/// Third-party libraries implement this — they do not own product types.
/// </summary>
public interface IMeshProcessor
{
    string BackendId { get; }
    RemeshMesh Process(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices, RemeshProfile profile);
}

public interface IUvUnwrapper
{
    string BackendId { get; }
    IReadOnlyList<Vector2> Unwrap(IReadOnlyList<Vector3> positions);
}

public sealed class SphericalUvUnwrapper : IUvUnwrapper
{
    public string BackendId => "spherical";
    public IReadOnlyList<Vector2> Unwrap(IReadOnlyList<Vector3> positions) => UvUnwrapper.Spherical(positions);
}

public sealed class VertexClusterMeshProcessor : IMeshProcessor
{
    private readonly IUvUnwrapper _uv;

    public VertexClusterMeshProcessor(IUvUnwrapper? uv = null) => _uv = uv ?? new SphericalUvUnwrapper();

    public string BackendId => MeshProcessorSelector.DefaultBackendId;

    public RemeshMesh Process(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices, RemeshProfile profile)
    {
        var mesh = RemeshPipeline.Run(positions, indices, profile);
        if (_uv.BackendId == "spherical")
            return mesh;

        var uvs = _uv.Unwrap(mesh.Positions);
        var report = MeshValidator.Validate(mesh.Positions, mesh.Indices, uvs.Count);
        if (report.Rejected)
            throw new InvalidOperationException(
                "UV backend produced a rejected mesh: " + string.Join("; ", report.Issues.Select(i => i.Code)));
        return new RemeshMesh
        {
            Positions = mesh.Positions,
            Indices = mesh.Indices,
            Uvs = uvs,
            Profile = profile,
            BackendId = BackendId + "+" + _uv.BackendId
        };
    }
}

/// <summary>
/// Optional QEM backend via geometry3Sharp Reducer (already a Mesh-project dependency).
/// Opt-in only — default product path remains vertex-cluster until proven better on shipping assets.
/// </summary>
public sealed class Geometry3SharpQemMeshProcessor : IMeshProcessor
{
    private readonly IUvUnwrapper _uv;

    public Geometry3SharpQemMeshProcessor(IUvUnwrapper? uv = null) => _uv = uv ?? new SphericalUvUnwrapper();

    public string BackendId => MeshProcessorSelector.Geometry3SharpBackendId;

    public RemeshMesh Process(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices, RemeshProfile profile)
    {
        if (positions.Count < 3 || indices.Count < 3)
            throw new InvalidOperationException("Remesh input has no triangles.");

        if (profile == RemeshProfile.KeepOriginal)
        {
            var uvsKeep = _uv.Unwrap(positions);
            return new RemeshMesh
            {
                Positions = positions.ToList(),
                Indices = indices.ToList(),
                Uvs = uvsKeep,
                Profile = profile,
                BackendId = BackendId
            };
        }

        var welded = RemeshPipeline.Weld(positions, indices);
        var sourceTris = welded.Indices.Count / 3;
        var ratio = RemeshPipeline.TargetRatio(profile);
        var targetTris = Math.Max(4, (int)MathF.Ceiling(sourceTris * Math.Clamp(ratio, 0.02f, 0.95f)));

        var gMesh = ToDMesh(welded.Positions, welded.Indices);
        if (gMesh.TriangleCount > targetTris)
        {
            var reducer = new Reducer(gMesh);
            reducer.ReduceToTriangleCount(targetTris);
        }

        var (pos, idx) = FromDMesh(gMesh);
        if (idx.Count < 3)
            throw new InvalidOperationException("geometry3Sharp Reducer produced an empty mesh.");

        var uvs = _uv.Unwrap(pos);
        var report = MeshValidator.Validate(pos, idx, uvs.Count);
        if (report.Rejected)
            throw new InvalidOperationException(
                "geometry3Sharp remesh rejected: " + string.Join("; ", report.Issues.Select(i => i.Code)));

        return new RemeshMesh
        {
            Positions = pos,
            Indices = idx,
            Uvs = uvs,
            Profile = profile,
            BackendId = BackendId
        };
    }

    private static DMesh3 ToDMesh(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices)
    {
        var mesh = new DMesh3();
        var map = new int[positions.Count];
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            map[i] = mesh.AppendVertex(new Vector3d(p.X, p.Y, p.Z));
        }
        for (var i = 0; i + 2 < indices.Count; i += 3)
        {
            var a = map[indices[i]];
            var b = map[indices[i + 1]];
            var c = map[indices[i + 2]];
            if (a == b || b == c || a == c)
                continue;
            var eid = mesh.AppendTriangle(a, b, c);
            if (eid < 0)
                mesh.AppendTriangle(a, c, b);
        }
        if (mesh.TriangleCount == 0)
            throw new InvalidOperationException("geometry3Sharp mesh has no triangles.");
        return mesh;
    }

    private static (List<Vector3> Positions, List<int> Indices) FromDMesh(DMesh3 mesh)
    {
        var remap = new Dictionary<int, int>();
        var positions = new List<Vector3>();
        foreach (var vid in mesh.VertexIndices())
        {
            var v = mesh.GetVertex(vid);
            remap[vid] = positions.Count;
            positions.Add(new Vector3((float)v.x, (float)v.y, (float)v.z));
        }
        var indices = new List<int>();
        foreach (var tid in mesh.TriangleIndices())
        {
            var t = mesh.GetTriangle(tid);
            if (!remap.ContainsKey(t.a) || !remap.ContainsKey(t.b) || !remap.ContainsKey(t.c))
                continue;
            var a = remap[t.a];
            var b = remap[t.b];
            var c = remap[t.c];
            if (a == b || b == c || a == c)
                continue;
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }
        return (positions, indices);
    }
}

public static class MeshProcessorSelector
{
    public const string DefaultBackendId = "vertex-cluster";
    public const string Geometry3SharpBackendId = "geometry3sharp-qem";

    public static IMeshProcessor Create(string? backendId = null, IUvUnwrapper? uv = null)
    {
        var id = string.IsNullOrWhiteSpace(backendId)
            ? Environment.GetEnvironmentVariable("THREEDGOD_MESH_PROCESSOR") ?? DefaultBackendId
            : backendId;
        return id.Trim().ToLowerInvariant() switch
        {
            Geometry3SharpBackendId or "g3" or "geometry3sharp" => new Geometry3SharpQemMeshProcessor(uv),
            _ => new VertexClusterMeshProcessor(uv)
        };
    }
}
