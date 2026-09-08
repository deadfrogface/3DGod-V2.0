using System.IO.Compression;
using System.Text;
using ThreeDGod.Application;
using ThreeDGod.Core.Domain;

namespace ThreeDGod.Persistence;

public sealed class GodProjectArchive : IProjectService
{
    public const string FormatName = "3dgod";
    public const int CurrentFormatVersion = 1;

    private readonly ArchiveLimits _limits;
    private readonly IReadOnlyList<IProjectMigration> _migrations;

    public GodProjectArchive()
        : this(new ArchiveLimits(), [])
    {
    }

    public GodProjectArchive(ArchiveLimits limits, IEnumerable<IProjectMigration>? migrations = null)
    {
        _limits = limits;
        _migrations = migrations?.ToArray() ?? [];
    }

    public async Task SaveAsync(ProjectBundle bundle, string destinationPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ProjectArchiveException("InvalidPath", "Destination path is empty.");

        var fullPath = Path.GetFullPath(destinationPath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ProjectArchiveException("InvalidPath", "Destination has no directory.");
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024, FileOptions.Asynchronous))
            {
                WriteBundle(stream, bundle);
                await stream.FlushAsync(cancellationToken);
            }

            ValidateFile(tempPath);

            if (File.Exists(fullPath))
            {
                var backup = fullPath + ".bak";
                File.Copy(fullPath, backup, overwrite: true);
                File.Replace(tempPath, fullPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, fullPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public Task<ProjectBundle> LoadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new ProjectArchiveException("MissingFile", $"Project file not found: {sourcePath}");

        using var stream = File.OpenRead(sourcePath);
        var bundle = ReadBundle(stream);
        return Task.FromResult(ApplyMigrations(bundle));
    }

    public void WriteBundle(Stream stream, ProjectBundle bundle)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
        var files = new List<(string Path, byte[] Bytes)>();

        void Add(string relative, object value)
        {
            var path = ArchivePathRules.NormalizeRelativePath(relative);
            var bytes = Encoding.UTF8.GetBytes(DomainJson.Serialize(value));
            files.Add((path, bytes));
        }

        Add("project.json", bundle.Project);
        foreach (var character in bundle.Characters)
            Add($"characters/{character.CharacterId:D}/character.json", character);
        foreach (var mesh in bundle.Meshes)
            Add($"assets/{mesh.MeshAssetId:D}/asset.json", mesh);
        foreach (var material in bundle.Materials)
            Add($"materials/{material.MaterialId:D}.json", material);
        foreach (var rig in bundle.Rigs)
            Add($"characters/rigs/{rig.RigId:D}/rig.json", rig);
        foreach (var def in bundle.GarmentDefinitions)
            Add($"garments/definitions/{def.GarmentDefinitionId:D}.json", def);
        foreach (var inst in bundle.GarmentInstances)
            Add($"garments/instances/{inst.GarmentInstanceId:D}/instance.json", inst);
        foreach (var att in bundle.Attachments)
            Add($"attachments/{att.AttachmentId:D}.json", att);

        var manifest = new GodProjectManifest
        {
            Format = FormatName,
            FormatVersion = CurrentFormatVersion,
            ProjectId = bundle.Project.ProjectId,
            RootProjectPath = "project.json",
            Zip64 = true,
            Files = files.Select(f => new ManifestFileEntry
            {
                Path = f.Path,
                Sha256 = ArchivePathRules.Sha256Hex(f.Bytes),
                Size = f.Bytes.Length
            }).ToList()
        };
        var manifestBytes = Encoding.UTF8.GetBytes(DomainJson.Serialize(manifest));
        WriteEntry(archive, "manifest.json", manifestBytes);
        foreach (var file in files)
            WriteEntry(archive, file.Path, file.Bytes);
    }

    public ProjectBundle ReadBundle(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > _limits.MaxFileCount)
            throw new ProjectArchiveException("TooManyFiles", $"Archive has {archive.Entries.Count} files; max is {_limits.MaxFileCount}.");

        long total = 0;
        var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith('/'))
                continue;

            var path = ArchivePathRules.NormalizeRelativePath(entry.FullName);
            if (entry.Length > _limits.MaxSingleEntryBytes)
                throw new ProjectArchiveException("EntryTooLarge", $"Entry {path} exceeds max size.");

            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            var bytes = ms.ToArray();
            total += bytes.Length;
            if (total > _limits.MaxDecompressedBytes)
                throw new ProjectArchiveException("ArchiveTooLarge", "Decompressed archive exceeds limit.");
            map[path] = bytes;
        }

        if (!map.TryGetValue("manifest.json", out var manifestBytes))
            throw new ProjectArchiveException("MissingManifest", "manifest.json is required.");

        GodProjectManifest manifest;
        try
        {
            manifest = DomainJson.Deserialize<GodProjectManifest>(Encoding.UTF8.GetString(manifestBytes));
        }
        catch (Exception)
        {
            throw new ProjectArchiveException("MalformedJson", "manifest.json is malformed.");
        }

        if (!string.Equals(manifest.Format, FormatName, StringComparison.Ordinal))
            throw new ProjectArchiveException("UnknownFormat", $"Unsupported format '{manifest.Format}'.");

        foreach (var file in manifest.Files)
        {
            var path = ArchivePathRules.NormalizeRelativePath(file.Path);
            if (!map.TryGetValue(path, out var bytes))
                throw new ProjectArchiveException("MissingFile", $"Manifest lists missing file: {path}");
            var actual = ArchivePathRules.Sha256Hex(bytes);
            if (!string.Equals(actual, file.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new ProjectArchiveException("HashMismatch", $"SHA256 mismatch for {path}.");
        }

        var bundle = new ProjectBundle
        {
            Project = ReadJson<ProjectDocument>(map, "project.json")
        };
        foreach (var (path, bytes) in map)
        {
            if (path.EndsWith("/character.json", StringComparison.OrdinalIgnoreCase))
                bundle.Characters.Add(ParseJson<CharacterDocument>(bytes, path));
            else if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/asset.json", StringComparison.OrdinalIgnoreCase))
                bundle.Meshes.Add(ParseJson<MeshAsset>(bytes, path));
            else if (path.StartsWith("materials/", StringComparison.OrdinalIgnoreCase) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                bundle.Materials.Add(ParseJson<MaterialDefinition>(bytes, path));
            else if (path.EndsWith("/rig.json", StringComparison.OrdinalIgnoreCase))
                bundle.Rigs.Add(ParseJson<RigDefinition>(bytes, path));
            else if (path.StartsWith("garments/definitions/", StringComparison.OrdinalIgnoreCase))
                bundle.GarmentDefinitions.Add(ParseJson<GarmentDefinition>(bytes, path));
            else if (path.StartsWith("garments/instances/", StringComparison.OrdinalIgnoreCase))
                bundle.GarmentInstances.Add(ParseJson<GarmentInstance>(bytes, path));
            else if (path.StartsWith("attachments/", StringComparison.OrdinalIgnoreCase))
                bundle.Attachments.Add(ParseJson<AttachmentInstance>(bytes, path));
        }

        return bundle;
    }

    public void ValidateFile(string path)
    {
        using var stream = File.OpenRead(path);
        _ = ReadBundle(stream);
    }

    private ProjectBundle ApplyMigrations(ProjectBundle bundle)
    {
        var version = bundle.Project.FormatVersion;
        var remaining = _migrations.OrderBy(m => m.FromVersion).ToList();
        while (version < CurrentFormatVersion)
        {
            var step = remaining.FirstOrDefault(m => m.FromVersion == version);
            if (step is null)
                throw new ProjectArchiveException("MigrationMissing", $"No migration from format version {version}.");
            bundle = step.MigrateAsync(bundle).GetAwaiter().GetResult();
            version = step.ToVersion;
            bundle.Project.FormatVersion = version;
        }

        return bundle;
    }

    private static void WriteEntry(ZipArchive archive, string path, byte[] bytes)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var s = entry.Open();
        s.Write(bytes, 0, bytes.Length);
    }

    private static T ReadJson<T>(IReadOnlyDictionary<string, byte[]> map, string path)
    {
        if (!map.TryGetValue(path, out var bytes))
            throw new ProjectArchiveException("MissingFile", $"{path} is required.");
        return ParseJson<T>(bytes, path);
    }

    private static T ParseJson<T>(byte[] bytes, string path)
    {
        try
        {
            return DomainJson.Deserialize<T>(Encoding.UTF8.GetString(bytes));
        }
        catch (Exception)
        {
            throw new ProjectArchiveException("MalformedJson", $"{path} is malformed JSON.");
        }
    }
}
