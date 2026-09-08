using System.Numerics;
using SharpGLTF.Schema2;

namespace ThreeDGod.Mesh;

public static class MeshCompare
{
    public static IReadOnlyList<Vector3> ReadPositions(string glbPath)
    {
        var model = ModelRoot.Load(glbPath);
        var positions = new List<Vector3>();
        foreach (var primitive in model.LogicalMeshes.SelectMany(m => m.Primitives))
        {
            var accessor = primitive.GetVertexAccessor("POSITION");
            if (accessor is null)
                continue;
            positions.AddRange(accessor.AsVector3Array().Select(v => new Vector3(v.X, v.Y, v.Z)));
        }
        return positions;
    }

    public static bool IsUniformScale(IReadOnlyList<Vector3> a, IReadOnlyList<Vector3> b, float tolerance = 0.02f)
    {
        if (a.Count == 0 || a.Count != b.Count)
            return false;
        float? scale = null;
        for (var i = 0; i < a.Count; i++)
        {
            var lenA = a[i].Length();
            var lenB = b[i].Length();
            if (lenA < 1e-5f)
                continue;
            var s = lenB / lenA;
            if (scale is null)
                scale = s;
            else if (MathF.Abs(s - scale.Value) > tolerance)
                return false;
            var predicted = a[i] * scale.Value;
            if (Vector3.Distance(predicted, b[i]) > MathF.Max(0.01f, lenA * tolerance))
                return false;
        }
        return scale is not null;
    }
}
