using System.Numerics;
using ThreeDGod.Infrastructure;
using ThreeDGod.Mesh;
using Xunit;

namespace ThreeDGodCreator.Core.Tests;

public sealed class MhcloFittingTests
{
    [Fact]
    public void DirectAndBarycentricMappings_FollowChangedMakeHumanBody()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-mhclo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var body = Path.Combine(root, "body.glb");
            TriangleMeshExport.WriteGlb(body,
                [new(0,0,0), new(2,0,0), new(0,2,0), new(0,0,2)],
                [0,1,2, 0,2,3]);
            var obj = Path.Combine(root, "hair.obj");
            File.WriteAllText(obj, "v 0 0 0\nv 0 0 0\nv 0 0 0\nf 1 2 3\n");
            var mhclo = Path.Combine(root, "hair.mhclo");
            File.WriteAllText(mhclo,
                "# synthetic topology proof\nverts 0\n0\n0 1 2 0.25 0.25 0.5 0.1 0 0\n3\n");
            var dest = Path.Combine(root, "fitted.glb");

            var result = new MhcloFittingService().Fit(body, obj, mhclo, dest);
            var pos = MeshCompare.ReadPositions(dest);
            Assert.Equal(3, result.MappingCount);
            Assert.Contains(pos, p => Vector3.Distance(p, new Vector3(0,0,0)) < 0.001f);
            Assert.Contains(pos, p => Vector3.Distance(p, new Vector3(0.6f,1f,0)) < 0.001f);
            Assert.Contains(pos, p => Vector3.Distance(p, new Vector3(0,0,2)) < 0.001f);
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }

    [Fact]
    public void InvalidTopologyIndex_FailsClosed()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-mhclo-bad-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var body = Path.Combine(root, "body.glb");
            TriangleMeshExport.WriteGlb(body, [new(0,0,0),new(1,0,0),new(0,1,0)], [0,1,2]);
            var obj = Path.Combine(root, "x.obj");
            File.WriteAllText(obj, "v 0 0 0\nv 0 0 0\nv 0 0 0\nf 1 2 3\n");
            var mhclo = Path.Combine(root, "x.mhclo");
            File.WriteAllText(mhclo, "verts 0\n0\n99\n2\n");
            Assert.Throws<InvalidDataException>(() => new MhcloFittingService().Fit(body,obj,mhclo,Path.Combine(root,"out.glb")));
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }
}
