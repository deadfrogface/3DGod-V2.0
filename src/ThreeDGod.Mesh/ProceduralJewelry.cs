using System.Numerics;

namespace ThreeDGod.Mesh;

public static class ProceduralJewelry
{
    public static (List<Vector3> Positions, List<int> Indices) SilverFrogNecklace()
    {
        var mesh = new MeshBuilder3D();
        const int links = 10;
        for (var i = 0; i < links; i++)
        {
            var t = i / (float)(links - 1);
            var angle = t * MathF.PI;
            var center = new Vector3(MathF.Cos(angle) * 0.22f, 0.18f + MathF.Sin(angle) * 0.08f, MathF.Sin(angle) * 0.04f);
            mesh.AddTorus(center, Vector3.UnitZ, ringRadius: 0.035f, tubeRadius: 0.008f, rings: 12, sides: 8);
        }

        var pendant = new Vector3(0, 0.02f, 0);
        mesh.AddSphere(pendant, 0.045f, 12, 8);
        mesh.AddSphere(pendant + new Vector3(-0.02f, 0.035f, 0.03f), 0.012f, 8, 6);
        mesh.AddSphere(pendant + new Vector3(0.02f, 0.035f, 0.03f), 0.012f, 8, 6);
        mesh.AddSphere(pendant + new Vector3(-0.03f, -0.03f, 0.02f), 0.012f, 6, 5);
        mesh.AddSphere(pendant + new Vector3(0.03f, -0.03f, 0.02f), 0.012f, 6, 5);
        mesh.AddSphere(pendant + new Vector3(-0.02f, -0.035f, -0.02f), 0.011f, 6, 5);
        mesh.AddSphere(pendant + new Vector3(0.02f, -0.035f, -0.02f), 0.011f, 6, 5);
        return (mesh.Positions, mesh.Indices);
    }
}
