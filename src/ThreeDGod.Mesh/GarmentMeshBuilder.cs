using System.Numerics;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Mesh;

public static class GarmentMeshBuilder
{
    public static (List<Vector3> Positions, List<int> Indices) Build(GarmentType type, IReadOnlyDictionary<string, float> parameters)
    {
        float P(string key, float fallback) => parameters.TryGetValue(key, out var v) ? v : fallback;
        var length = P("length", 0.55f);
        var sleeve = P("sleeveLength", 0.45f);
        var width = P("width", 0.28f);
        var collar = P("collar", 0.08f);
        var builder = new MeshBuilder3D();
        switch (type)
        {
            case GarmentType.Pants:
            case GarmentType.Shorts:
                builder.AddSphere(new Vector3(-0.1f, 0.5f - length, 0), 0.07f + width * 0.1f, 10, 8);
                builder.AddSphere(new Vector3(0.1f, 0.5f - length, 0), 0.07f + width * 0.1f, 10, 8);
                builder.AddSphere(new Vector3(0, 0.75f, 0), 0.12f + width * 0.15f, 12, 8);
                break;
            case GarmentType.Skirt:
            case GarmentType.Dress:
                builder.AddSphere(new Vector3(0, 0.9f, 0), 0.16f + width, 14, 10);
                builder.AddCone(new Vector3(0, 0.9f, 0), new Vector3(0, 0.9f - length, 0), 0.12f + width, 14);
                if (type == GarmentType.Dress)
                    AddSleeves(builder, sleeve, width, y: 1.15f);
                break;
            default:
                builder.AddSphere(new Vector3(0, 1.15f, 0), 0.16f + width * 0.2f, 14, 10);
                builder.AddSphere(new Vector3(0, 1.15f - length * 0.35f, 0), 0.14f + width * 0.2f, 12, 8);
                AddSleeves(builder, sleeve, width, y: 1.2f);
                if (type is GarmentType.Jacket or GarmentType.Coat)
                    builder.AddSphere(new Vector3(0, 1.32f + collar, 0), 0.09f + collar, 10, 8);
                break;
        }
        return (builder.Positions, builder.Indices);
    }

    private static void AddSleeves(MeshBuilder3D builder, float sleeve, float width, float y)
    {
        var extent = 0.22f + sleeve;
        builder.AddCone(new Vector3(-0.16f, y, 0), new Vector3(-extent, y - 0.05f, 0), 0.05f + width * 0.05f, 10);
        builder.AddCone(new Vector3(0.16f, y, 0), new Vector3(extent, y - 0.05f, 0), 0.05f + width * 0.05f, 10);
    }
}
