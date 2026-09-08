using System.Numerics;

namespace ThreeDGod.Mesh;

public sealed class MeshBuilder3D
{
    public List<Vector3> Positions { get; } = [];
    public List<int> Indices { get; } = [];

    public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        var i = Positions.Count;
        Positions.Add(a);
        Positions.Add(b);
        Positions.Add(c);
        Indices.Add(i);
        Indices.Add(i + 1);
        Indices.Add(i + 2);
    }

    public void AddSphere(Vector3 center, float radius, int slices, int stacks)
    {
        var start = Positions.Count;
        for (var lat = 0; lat <= stacks; lat++)
        {
            var theta = MathF.PI * lat / stacks;
            var y = MathF.Cos(theta);
            var r = MathF.Sin(theta);
            for (var lon = 0; lon <= slices; lon++)
            {
                var phi = 2 * MathF.PI * lon / slices;
                Positions.Add(center + new Vector3(r * MathF.Cos(phi), y, r * MathF.Sin(phi)) * radius);
            }
        }
        for (var lat = 0; lat < stacks; lat++)
        {
            for (var lon = 0; lon < slices; lon++)
            {
                var a = start + lat * (slices + 1) + lon;
                var b = a + slices + 1;
                Indices.Add(a);
                Indices.Add(b);
                Indices.Add(a + 1);
                Indices.Add(a + 1);
                Indices.Add(b);
                Indices.Add(b + 1);
            }
        }
    }

    public void AddTorus(Vector3 center, Vector3 axis, float ringRadius, float tubeRadius, int rings, int sides)
    {
        axis = Vector3.Normalize(axis);
        var tangent = Vector3.Normalize(Vector3.Cross(MathF.Abs(axis.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX, axis));
        var bitangent = Vector3.Normalize(Vector3.Cross(axis, tangent));
        var start = Positions.Count;
        for (var i = 0; i <= rings; i++)
        {
            var u = 2 * MathF.PI * i / rings;
            var ringCenter = center + (tangent * MathF.Cos(u) + bitangent * MathF.Sin(u)) * ringRadius;
            var normal = Vector3.Normalize(ringCenter - center);
            var bin = Vector3.Normalize(Vector3.Cross(axis, normal));
            for (var j = 0; j <= sides; j++)
            {
                var v = 2 * MathF.PI * j / sides;
                Positions.Add(ringCenter + (normal * MathF.Cos(v) + bin * MathF.Sin(v)) * tubeRadius);
            }
        }
        for (var i = 0; i < rings; i++)
        {
            for (var j = 0; j < sides; j++)
            {
                var a = start + i * (sides + 1) + j;
                var b = a + sides + 1;
                Indices.Add(a);
                Indices.Add(b);
                Indices.Add(a + 1);
                Indices.Add(a + 1);
                Indices.Add(b);
                Indices.Add(b + 1);
            }
        }
    }

    public void AddCone(Vector3 baseCenter, Vector3 tip, float radius, int slices)
    {
        var axis = tip - baseCenter;
        var height = axis.Length();
        if (height < 1e-6f)
            return;
        axis /= height;
        var tangent = Vector3.Normalize(Vector3.Cross(MathF.Abs(axis.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX, axis));
        var bitangent = Vector3.Normalize(Vector3.Cross(axis, tangent));
        var tipIndex = Positions.Count;
        Positions.Add(tip);
        var ringStart = Positions.Count;
        for (var i = 0; i < slices; i++)
        {
            var a = 2 * MathF.PI * i / slices;
            Positions.Add(baseCenter + (tangent * MathF.Cos(a) + bitangent * MathF.Sin(a)) * radius);
        }
        for (var i = 0; i < slices; i++)
        {
            Indices.Add(tipIndex);
            Indices.Add(ringStart + i);
            Indices.Add(ringStart + (i + 1) % slices);
        }
    }
}
