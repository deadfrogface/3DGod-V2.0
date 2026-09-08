using System.Globalization;
using System.Numerics;

namespace ThreeDGod.Mesh;

public sealed class ImportedMesh
{
    public List<Vector3> Positions { get; } = [];
    public List<int> Indices { get; } = [];
    public string SourcePath { get; init; } = "";
    public string SourceFormat { get; init; } = "";
}

public static class ObjImporter
{
    public static ImportedMesh ImportObj(string path)
    {
        var mesh = new ImportedMesh { SourcePath = path, SourceFormat = "obj" };
        var verts = new List<Vector3>();
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.StartsWith("v "))
            {
                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                verts.Add(new Vector3(
                    float.Parse(p[1], CultureInfo.InvariantCulture),
                    float.Parse(p[2], CultureInfo.InvariantCulture),
                    float.Parse(p[3], CultureInfo.InvariantCulture)));
            }
            else if (line.StartsWith("f "))
            {
                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var ids = p.Skip(1).Select(s => int.Parse(s.Split('/')[0], CultureInfo.InvariantCulture) - 1).ToArray();
                if (ids.Length >= 3)
                {
                    mesh.Indices.Add(ids[0]);
                    mesh.Indices.Add(ids[1]);
                    mesh.Indices.Add(ids[2]);
                }
            }
        }
        mesh.Positions.AddRange(verts);
        return mesh;
    }
}

public sealed class AssimpImportGate
{
    public static string Status { get; } = "NotInstalled";

    public static ImportedMesh Import(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".obj")
            return ObjImporter.ImportObj(path);
        throw new InvalidOperationException("NotInstalled – Assimp native import is not available. OBJ is supported via the built-in parser.");
    }
}
