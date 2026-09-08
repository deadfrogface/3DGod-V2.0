using System.IO.Compression;
using System.Text;
using ThreeDGod.Core.Domain;
using ThreeDGod.Persistence;

namespace ThreeDGodCreator.Core.Tests;

public class GodProjectArchiveTests
{
    [Fact]
    public async Task Save_Close_Load_PreservesDomainIdentity()
    {
        var bundle = SampleBundle();
        var archive = new GodProjectArchive();
        var path = Path.Combine(Path.GetTempPath(), $"3dgod-{Guid.NewGuid():N}.3dgod");
        try
        {
            await archive.SaveAsync(bundle, path);
            Assert.True(File.Exists(path));
            var loaded = await archive.LoadAsync(path);
            Assert.Equal(bundle.Project.ProjectId, loaded.Project.ProjectId);
            Assert.Equal(bundle.Project.Name, loaded.Project.Name);
            Assert.Equal(bundle.Characters[0].CharacterId, loaded.Characters[0].CharacterId);
            Assert.Equal(bundle.Meshes[0].MeshAssetId, loaded.Meshes[0].MeshAssetId);
            Assert.Equal(bundle.Materials[0].MaterialId, loaded.Materials[0].MaterialId);
            Assert.Equal(bundle.Rigs[0].RigId, loaded.Rigs[0].RigId);
            Assert.Equal(bundle.GarmentDefinitions[0].GarmentDefinitionId, loaded.GarmentDefinitions[0].GarmentDefinitionId);
            Assert.Equal(bundle.GarmentInstances[0].GarmentInstanceId, loaded.GarmentInstances[0].GarmentInstanceId);
            Assert.Equal(bundle.Attachments[0].AttachmentId, loaded.Attachments[0].AttachmentId);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }
    }

    [Fact]
    public async Task Save_ReplacesAtomically_AndWritesBackup()
    {
        var archive = new GodProjectArchive();
        var path = Path.Combine(Path.GetTempPath(), $"3dgod-{Guid.NewGuid():N}.3dgod");
        try
        {
            var first = SampleBundle();
            first.Project.Name = "first";
            await archive.SaveAsync(first, path);
            var second = SampleBundle();
            second.Project.Name = "second";
            await archive.SaveAsync(second, path);
            var loaded = await archive.LoadAsync(path);
            Assert.Equal("second", loaded.Project.Name);
            Assert.True(File.Exists(path + ".bak"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }
    }

    [Fact]
    public void ZipSlip_ParentSegment_IsRejected()
    {
        var ex = Assert.Throws<ProjectArchiveException>(() =>
            ReadMaliciousZip(("../escape.json", "{}" )));
        Assert.Equal("ZipSlip", ex.Code);
    }

    [Fact]
    public void AbsolutePath_IsRejected()
    {
        Assert.Throws<ProjectArchiveException>(() => ArchivePathRules.NormalizeRelativePath("/tmp/evil.json"));
        Assert.Throws<ProjectArchiveException>(() => ArchivePathRules.NormalizeRelativePath("C:\\Windows\\evil.json"));
    }

    [Fact]
    public void OversizeEntry_IsRejected()
    {
        var archive = new GodProjectArchive(new ArchiveLimits { MaxSingleEntryBytes = 8 });
        var ex = Assert.Throws<ProjectArchiveException>(() =>
        {
            using var ms = BuildZip(("manifest.json", "{}"), ("project.json", new string('x', 32)));
            archive.ReadBundle(ms);
        });
        Assert.Equal("EntryTooLarge", ex.Code);
    }

    [Fact]
    public void TooManyFiles_IsRejected()
    {
        var archive = new GodProjectArchive(new ArchiveLimits { MaxFileCount = 2 });
        var ex = Assert.Throws<ProjectArchiveException>(() =>
        {
            using var ms = BuildZip(("a.json", "{}"), ("b.json", "{}"), ("c.json", "{}"));
            archive.ReadBundle(ms);
        });
        Assert.Equal("TooManyFiles", ex.Code);
    }

    [Fact]
    public void MalformedJson_IsRejected()
    {
        var archive = new GodProjectArchive();
        var ex = Assert.Throws<ProjectArchiveException>(() =>
        {
            using var ms = BuildZip(("manifest.json", "{not-json"));
            archive.ReadBundle(ms);
        });
        Assert.Equal("MalformedJson", ex.Code);
    }

    [Fact]
    public void MissingManifest_IsRejected()
    {
        var archive = new GodProjectArchive();
        var ex = Assert.Throws<ProjectArchiveException>(() =>
        {
            using var ms = BuildZip(("project.json", "{}"));
            archive.ReadBundle(ms);
        });
        Assert.Equal("MissingManifest", ex.Code);
    }

    [Fact]
    public void IProjectMigration_Interface_ExistsWithVersionContract()
    {
        IProjectMigration migration = new NoopMigration();
        Assert.Equal(1, migration.FromVersion);
        Assert.Equal(1, migration.ToVersion);
    }

    private static ProjectBundle SampleBundle()
    {
        var project = new ProjectDocument { Name = "Roundtrip" };
        var character = new CharacterDocument { Name = "Hero" };
        var mesh = new MeshAsset { Name = "body", CanonicalGlbPath = "assets/body.glb" };
        var material = new MaterialDefinition { Name = "skin" };
        var rig = new RigDefinition { Name = "humanoid", IsHumanoid = true };
        var garmentDef = new GarmentDefinition { Name = "shirt", GarmentType = GarmentType.Shirt };
        var garment = new GarmentInstance
        {
            DefinitionId = garmentDef.GarmentDefinitionId,
            CharacterId = character.CharacterId
        };
        var attachment = new AttachmentInstance
        {
            CharacterId = character.CharacterId,
            AssetId = mesh.MeshAssetId,
            AttachmentType = AttachmentType.Necklace
        };
        project.CharacterIds.Add(character.CharacterId);
        project.AssetIds.Add(mesh.MeshAssetId);
        return new ProjectBundle
        {
            Project = project,
            Characters = [character],
            Meshes = [mesh],
            Materials = [material],
            Rigs = [rig],
            GarmentDefinitions = [garmentDef],
            GarmentInstances = [garment],
            Attachments = [attachment]
        };
    }

    private static void ReadMaliciousZip(params (string Path, string Content)[] entries)
    {
        var archive = new GodProjectArchive();
        using var ms = BuildZip(entries);
        archive.ReadBundle(ms);
    }

    private static MemoryStream BuildZip(params (string Path, string Content)[] entries)
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in entries)
            {
                var entry = zip.CreateEntry(path, CompressionLevel.NoCompression);
                using var s = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(content);
                s.Write(bytes, 0, bytes.Length);
            }
        }
        ms.Position = 0;
        return ms;
    }

    private sealed class NoopMigration : IProjectMigration
    {
        public int FromVersion => 1;
        public int ToVersion => 1;
        public Task<ProjectBundle> MigrateAsync(ProjectBundle source, CancellationToken cancellationToken = default) =>
            Task.FromResult(source);
    }
}
