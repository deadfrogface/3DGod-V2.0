using System.Numerics;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using ThreeDGod.Mesh;

namespace ThreeDGod.Rigging;

/// <summary>
/// Nearest-vertex weight transfer from a skinned body onto a garment mesh.
/// Same skeleton, linear-blend skinning. Not automatic rig generation.
/// </summary>
public static class GarmentSkinBinder
{
    public static string Bind(string skinnedBodyGlb, string garmentGlb, string destinationGlb)
    {
        if (!File.Exists(skinnedBodyGlb))
            throw new InvalidOperationException("Skinned body GLB is missing. No garment skin will be faked.");
        if (!File.Exists(garmentGlb))
            throw new InvalidOperationException("Garment GLB is missing. No garment skin will be faked.");

        var body = ModelRoot.Load(skinnedBodyGlb);
        if (body.LogicalSkins.Count == 0)
            throw new InvalidOperationException("Body has no skin. Weight transfer needs JOINTS_0/WEIGHTS_0.");

        var prim = body.LogicalMeshes.SelectMany(m => m.Primitives)
            .FirstOrDefault(p => p.GetVertexAccessor("JOINTS_0") != null && p.GetVertexAccessor("WEIGHTS_0") != null)
            ?? throw new InvalidOperationException("Body primitive has no JOINTS_0/WEIGHTS_0.");

        var bodyPos = prim.GetVertexAccessor("POSITION")!.AsVector3Array()
            .Select(v => new Vector3(v.X, v.Y, v.Z)).ToList();
        var bodyJoints = prim.GetVertexAccessor("JOINTS_0")!.AsVector4Array()
            .Select(v => new Vector4(v.X, v.Y, v.Z, v.W)).ToList();
        var bodyWeights = prim.GetVertexAccessor("WEIGHTS_0")!.AsVector4Array()
            .Select(v => new Vector4(v.X, v.Y, v.Z, v.W)).ToList();
        var (garmentPos, garmentIdx) = MeshCompare.ReadMesh(garmentGlb);
        if (garmentPos.Count < 3 || garmentIdx.Count < 3)
            throw new InvalidOperationException("Garment mesh has no triangles.");

        var skin = body.LogicalSkins[0];
        var joints = CloneSkinJoints(skin);

        var material = new MaterialBuilder("garment-skin")
            .WithDoubleSide(true)
            .WithMetallicRoughnessShader();
        var mesh = new MeshBuilder<VertexPosition, VertexEmpty, VertexJoints4>("garment");
        var primitive = mesh.UsePrimitive(material);
        for (var i = 0; i + 2 < garmentIdx.Count; i += 3)
        {
            var a = garmentIdx[i];
            var b = garmentIdx[i + 1];
            var c = garmentIdx[i + 2];
            primitive.AddTriangle(
                SkinnedVertex(garmentPos[a], bodyPos, bodyJoints, bodyWeights),
                SkinnedVertex(garmentPos[b], bodyPos, bodyJoints, bodyWeights),
                SkinnedVertex(garmentPos[c], bodyPos, bodyJoints, bodyWeights));
        }

        var scene = new SceneBuilder();
        scene.AddSkinnedMesh(mesh, Matrix4x4.Identity, joints);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationGlb))!);
        scene.ToGltf2().SaveGLB(destinationGlb);
        if (!File.Exists(destinationGlb) || new FileInfo(destinationGlb).Length < 64)
            throw new InvalidOperationException("Skinned garment GLB was not written.");
        return destinationGlb;
    }

    private static VertexBuilder<VertexPosition, VertexEmpty, VertexJoints4> SkinnedVertex(
        Vector3 p,
        IReadOnlyList<Vector3> bodyPos,
        IReadOnlyList<Vector4> bodyJoints,
        IReadOnlyList<Vector4> bodyWeights)
    {
        var nearest = 0;
        var best = float.MaxValue;
        for (var i = 0; i < bodyPos.Count; i++)
        {
            var d = Vector3.DistanceSquared(p, bodyPos[i]);
            if (d < best)
            {
                best = d;
                nearest = i;
            }
        }
        var j = bodyJoints[nearest];
        var w = bodyWeights[nearest];
        var sparse = new[]
        {
            ((int)MathF.Round(j.X), w.X),
            ((int)MathF.Round(j.Y), w.Y),
            ((int)MathF.Round(j.Z), w.Z),
            ((int)MathF.Round(j.W), w.W)
        }.Where(t => t.Item2 > 1e-6f).ToArray();
        var bindings = sparse.Length == 0
            ? new VertexJoints4((0, 1f))
            : sparse.Length switch
            {
                1 => new VertexJoints4(sparse[0]),
                2 => new VertexJoints4(sparse[0], sparse[1]),
                3 => new VertexJoints4(sparse[0], sparse[1], sparse[2]),
                _ => new VertexJoints4(sparse[0], sparse[1], sparse[2], sparse[3])
            };
        return new VertexBuilder<VertexPosition, VertexEmpty, VertexJoints4>(new VertexPosition(p), default, bindings);
    }

    private static NodeBuilder[] CloneSkinJoints(Skin skin)
    {
        var jointNodes = new Node[skin.JointsCount];
        for (var i = 0; i < skin.JointsCount; i++)
            jointNodes[i] = skin.GetJoint(i).Joint;

        var builders = new NodeBuilder?[skin.JointsCount];
        NodeBuilder Ensure(int index)
        {
            if (builders[index] is { } existing)
                return existing;
            var node = jointNodes[index];
            var parent = node.VisualParent;
            var parentIndex = -1;
            if (parent is not null)
            {
                for (var i = 0; i < jointNodes.Length; i++)
                {
                    if (ReferenceEquals(jointNodes[i], parent))
                    {
                        parentIndex = i;
                        break;
                    }
                }
            }

            var name = string.IsNullOrWhiteSpace(node.Name) ? $"joint{index}" : node.Name;
            var nb = parentIndex >= 0 ? Ensure(parentIndex).CreateNode(name) : new NodeBuilder(name);
            var t = node.LocalTransform;
            nb.UseTranslation().Value = t.Translation;
            nb.UseRotation().Value = t.Rotation;
            nb.UseScale().Value = t.Scale;
            builders[index] = nb;
            return nb;
        }

        for (var i = 0; i < skin.JointsCount; i++)
            Ensure(i);
        return builders.Select(b => b!).ToArray();
    }
}
