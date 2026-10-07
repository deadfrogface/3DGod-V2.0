using System.Numerics;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;
using ThreeDGod.Mesh;
using ThreeDGod.Persistence;
using Xunit;

namespace ThreeDGodCreator.Core.Tests;

public sealed class CharacterAttachmentWorkflowTests
{
    [Fact]
    public async Task Attachment_IsEmbedded_Saved_Reopened_AndMaterializedAsScenePart()
    {
        var root = Path.Combine(Path.GetTempPath(), "3dgod-attachment-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var body = Path.Combine(root, "body.glb");
            var necklace = Path.Combine(root, "necklace.glb");
            WriteTriangle(body, 0);
            WriteTriangle(necklace, 0.2f);

            var session = new ActiveProjectSession();
            session.NewProject("AttachmentTest");
            session.SetActiveMeshFromGlbFile(body, "body");
            var attachment = session.AddAttachmentFromGlb(
                necklace, "Test necklace", AttachmentType.Necklace,
                new GeneratedAssetMetadata { BackendId = "test", LicenseProfileId = "CC0-1.0" });

            Assert.Equal("neck", attachment.ParentBoneSemantic);
            Assert.Contains(session.ListSceneParts(), x => x.Role == "attachment" && x.MeshAssetId == attachment.AssetId);

            var archive = new GodProjectArchive();
            var file = Path.Combine(root, "attachment.3dgod");
            await archive.SaveAsync(session.Snapshot(), file);

            var loaded = await archive.LoadAsync(file);
            var reopened = new ActiveProjectSession();
            reopened.LoadFrom(loaded, file);
            Assert.Single(reopened.Bundle.Attachments);
            var parts = reopened.MaterializeSceneGlbs(Path.Combine(root, "materialized"));
            Assert.Contains(parts, x => x.Role == "attachment" && File.Exists(x.GlbPath));
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static void WriteTriangle(string path, float x)
    {
        TriangleMeshExport.WriteGlb(
            path,
            [new Vector3(x, 0, 0), new Vector3(x + 0.1f, 0, 0), new Vector3(x, 0.1f, 0)],
            [0, 1, 2]);
    }
}
