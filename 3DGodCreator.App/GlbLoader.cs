using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SharpGLTF.Schema2;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

public static class GlbLoader
{
    public static Model3DGroup? Load(string path)
    {
        try
        {
            var model = ModelRoot.Load(path);
            if (model?.LogicalMeshes == null) return null;

            var group = new Model3DGroup();
            var rotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180));
            group.Transform = rotate;
            foreach (var mesh in model.LogicalMeshes)
            {
                foreach (var primitive in mesh.Primitives)
                {
                    var geo = BuildMeshGeometry(primitive);
                    if (geo != null)
                    {
                        var mat = new DiffuseMaterial(new SolidColorBrush(Colors.LightGray));
                        group.Children.Add(new GeometryModel3D(geo, mat));
                    }
                }
            }
            return group.Children.Count > 0 ? group : null;
        }
        catch (Exception ex)
        {
            AppLogger.Write($"[GlbLoader] Load failed: {path} - {ex.Message}", isError: true);
            AppLogger.LogException(ex, "GlbLoader.Load");
            DebugLog.Write($"[GlbLoader] Fehler: {ex.Message}");
            return null;
        }
    }

    private static MeshGeometry3D? BuildMeshGeometry(MeshPrimitive primitive)
    {
        var posAccessor = primitive.GetVertexAccessor("POSITION");
        if (posAccessor == null) return null;

        var positions = new Point3DCollection();
        foreach (var v in posAccessor.AsVector3Array())
            positions.Add(new Point3D(v.X, v.Y, v.Z));

        var indices = new Int32Collection();
        var indexAccessor = primitive.GetIndexAccessor();
        if (indexAccessor != null)
        {
            foreach (var i in indexAccessor.AsIndicesArray())
                indices.Add((int)i);
        }
        else
        {
            for (int i = 0; i < positions.Count; i++)
                indices.Add(i);
        }

        var normAccessor = primitive.GetVertexAccessor("NORMAL");
        var normals = new Vector3DCollection();
        if (normAccessor != null)
        {
            foreach (var n in normAccessor.AsVector3Array())
                normals.Add(new Vector3D(n.X, n.Y, n.Z));
        }

        var geo = new MeshGeometry3D
        {
            Positions = positions,
            TriangleIndices = indices
        };
        if (normals.Count == positions.Count)
            geo.Normals = normals;

        return geo;
    }
}
