using ThreeDGod.Core.Domain;

namespace ThreeDGod.Application;

/// <summary>
/// Single authoritative in-memory project state for the product UI.
/// Extends ProjectBundle / CharacterDocument — does not invent a parallel persistence model.
/// </summary>
public sealed class ActiveProjectSession
{
    private readonly object _gate = new();
    private ProjectBundle _bundle;
    private Guid? _activeCharacterId;
    private string? _projectPath;
    private bool _dirty;
    private string? _materializedMeshPath;

    public ActiveProjectSession()
    {
        _bundle = CreateEmpty("Untitled");
        _activeCharacterId = _bundle.Characters[0].CharacterId;
    }

    public event Action? Changed;

    public ProjectBundle Bundle
    {
        get { lock (_gate) return Clone(_bundle); }
    }

    public Guid? ActiveCharacterId
    {
        get { lock (_gate) return _activeCharacterId; }
    }

    public string? ProjectPath
    {
        get { lock (_gate) return _projectPath; }
    }

    public bool IsDirty
    {
        get { lock (_gate) return _dirty; }
    }

    public CharacterDocument? ActiveCharacter
    {
        get
        {
            lock (_gate)
            {
                if (_activeCharacterId is not Guid id)
                    return null;
                return _bundle.Characters.FirstOrDefault(c => c.CharacterId == id);
            }
        }
    }

    public bool IsAnnyHumanActive
    {
        get
        {
            var c = ActiveCharacter;
            return c is { CharacterKind: CharacterKind.ParametricHuman, SourceRepresentation: SourceRepresentation.AnnyParameters };
        }
    }

    public void NewProject(string name = "Untitled")
    {
        lock (_gate)
        {
            _bundle = CreateEmpty(name);
            _activeCharacterId = _bundle.Characters[0].CharacterId;
            _projectPath = null;
            _dirty = false;
            _materializedMeshPath = null;
        }
        RaiseChanged();
    }

    public void LoadFrom(ProjectBundle bundle, string? projectPath)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        lock (_gate)
        {
            _bundle = Clone(bundle);
            if (_bundle.Characters.Count == 0)
            {
                var character = NewHumanCharacter(bundle.Project.Name);
                _bundle.Characters.Add(character);
                _bundle.Project.CharacterIds = [character.CharacterId];
            }

            _activeCharacterId = _bundle.Project.CharacterIds.FirstOrDefault();
            if (_activeCharacterId == Guid.Empty || _bundle.Characters.All(c => c.CharacterId != _activeCharacterId))
                _activeCharacterId = _bundle.Characters[0].CharacterId;

            _projectPath = string.IsNullOrWhiteSpace(projectPath) ? null : Path.GetFullPath(projectPath);
            _dirty = false;
            _materializedMeshPath = null;
        }
        RaiseChanged();
    }

    public ProjectBundle Snapshot()
    {
        lock (_gate)
            return Clone(_bundle);
    }

    public void MarkClean(string? projectPath = null)
    {
        lock (_gate)
        {
            if (!string.IsNullOrWhiteSpace(projectPath))
                _projectPath = Path.GetFullPath(projectPath);
            _dirty = false;
        }
        RaiseChanged();
    }

    public void MarkDirty()
    {
        lock (_gate)
            _dirty = true;
        RaiseChanged();
    }

    public void SetAnnyState(ParametricHumanState state, bool markDirty = true)
    {
        ArgumentNullException.ThrowIfNull(state);
        lock (_gate)
        {
            var character = RequireActiveCharacter();
            character.CharacterKind = CharacterKind.ParametricHuman;
            character.SourceRepresentation = SourceRepresentation.AnnyParameters;
            character.ParametricHumanState = CloneState(state);
            character.EditRevision++;
            Touch(character);
            if (markDirty)
                _dirty = true;
        }
        RaiseChanged();
    }

    public void SetActiveMeshFromGlbFile(string glbPath, string name = "body", SourceRepresentation? source = null)
    {
        if (string.IsNullOrWhiteSpace(glbPath) || !File.Exists(glbPath))
            throw new FileNotFoundException("Active mesh GLB not found.", glbPath);

        var bytes = File.ReadAllBytes(glbPath);
        SetActiveMeshBytes(bytes, name, source ?? InferSource(), glbPath);
    }

    public void SetActiveMeshBytes(byte[] glbBytes, string name = "body", SourceRepresentation source = SourceRepresentation.GeneratedMesh, string? sourcePathHint = null)
    {
        ArgumentNullException.ThrowIfNull(glbBytes);
        if (glbBytes.Length < 12)
            throw new InvalidOperationException("GLB payload too small to be a real mesh.");

        lock (_gate)
        {
            var character = RequireActiveCharacter();
            var meshId = character.MeshSet.MeshAssetIds.FirstOrDefault();
            MeshAsset mesh;
            if (meshId != Guid.Empty)
            {
                mesh = _bundle.Meshes.FirstOrDefault(m => m.MeshAssetId == meshId) ?? new MeshAsset { MeshAssetId = meshId };
                if (!_bundle.Meshes.Contains(mesh))
                    _bundle.Meshes.Add(mesh);
            }
            else
            {
                mesh = new MeshAsset { MeshAssetId = Guid.NewGuid() };
                _bundle.Meshes.Add(mesh);
                character.MeshSet.MeshAssetIds = [mesh.MeshAssetId];
                if (!_bundle.Project.AssetIds.Contains(mesh.MeshAssetId))
                    _bundle.Project.AssetIds.Add(mesh.MeshAssetId);
            }

            mesh.Name = name;
            mesh.SourceFormat = "glb";
            mesh.CanonicalGlbPath = $"assets/{mesh.MeshAssetId:D}/mesh.glb";
            mesh.SourceHash = ArchivePathRulesSha256(glbBytes);
            mesh.ValidationState = "embedded";
            if (!string.IsNullOrWhiteSpace(sourcePathHint))
            {
                mesh.GeneratedMetadata ??= new GeneratedAssetMetadata();
                mesh.GeneratedMetadata.Prompt = "source:" + sourcePathHint;
            }

            _bundle.MeshBytes[mesh.MeshAssetId] = glbBytes.ToArray();
            character.SourceRepresentation = character.ParametricHumanState is not null
                ? SourceRepresentation.AnnyParameters
                : source;
            character.EditRevision++;
            Touch(character);
            _materializedMeshPath = null;
            _dirty = true;
        }
        RaiseChanged();
    }

    public string? TryMaterializeActiveMesh(string workRoot)
    {
        Directory.CreateDirectory(workRoot);
        lock (_gate)
        {
            var character = ActiveCharacterUnlocked();
            if (character is null)
                return null;
            var meshId = character.MeshSet.MeshAssetIds.FirstOrDefault();
            if (meshId == Guid.Empty)
                return null;
            if (!_bundle.MeshBytes.TryGetValue(meshId, out var bytes) || bytes.Length == 0)
            {
                var mesh = _bundle.Meshes.FirstOrDefault(m => m.MeshAssetId == meshId);
                if (mesh is not null && !string.IsNullOrWhiteSpace(mesh.CanonicalGlbPath) && File.Exists(mesh.CanonicalGlbPath))
                    return mesh.CanonicalGlbPath;
                return null;
            }

            var dest = Path.Combine(workRoot, $"{meshId:N}.glb");
            File.WriteAllBytes(dest, bytes);
            _materializedMeshPath = dest;
            var asset = _bundle.Meshes.FirstOrDefault(m => m.MeshAssetId == meshId);
            if (asset is not null)
                asset.CanonicalGlbPath = dest;
            return dest;
        }
    }

    public string? GetActiveMeshGlbPathOrMaterialize(string workRoot)
    {
        lock (_gate)
        {
            if (!string.IsNullOrWhiteSpace(_materializedMeshPath) && File.Exists(_materializedMeshPath))
                return _materializedMeshPath;
        }
        return TryMaterializeActiveMesh(workRoot);
    }

    public void UpsertMaterial(string slotName, float r, float g, float b, float a, float metallic, float roughness)
    {
        lock (_gate)
        {
            var character = RequireActiveCharacter();
            var existingId = character.MaterialSet.MaterialIds.FirstOrDefault();
            MaterialDefinition mat;
            if (existingId != Guid.Empty)
            {
                mat = _bundle.Materials.FirstOrDefault(m => m.MaterialId == existingId)
                      ?? new MaterialDefinition { MaterialId = existingId };
                if (!_bundle.Materials.Contains(mat))
                    _bundle.Materials.Add(mat);
            }
            else
            {
                mat = new MaterialDefinition { MaterialId = Guid.NewGuid() };
                _bundle.Materials.Add(mat);
                character.MaterialSet.MaterialIds = [mat.MaterialId];
            }

            mat.Name = string.IsNullOrWhiteSpace(slotName) ? "skin" : slotName;
            mat.BaseColorFactor = new ColorRgba { R = r, G = g, B = b, A = a };
            mat.MetallicFactor = metallic;
            mat.RoughnessFactor = roughness;
            mat.Provenance = "material-editor";
            character.EditRevision++;
            Touch(character);
            _dirty = true;
        }
        RaiseChanged();
    }

    public void AddFittedGarment(string fittedGlbPath, string presetName, ClippingReport? report = null)
    {
        if (string.IsNullOrWhiteSpace(fittedGlbPath) || !File.Exists(fittedGlbPath))
            throw new FileNotFoundException("Fitted garment GLB not found.", fittedGlbPath);

        var bytes = File.ReadAllBytes(fittedGlbPath);
        lock (_gate)
        {
            var character = RequireActiveCharacter();
            var def = new GarmentDefinition
            {
                Name = string.IsNullOrWhiteSpace(presetName) ? "jacket" : presetName,
                GarmentType = GarmentType.Jacket,
                GeneratorBackend = "garmentcode+geometry3Sharp"
            };
            var mesh = new MeshAsset
            {
                Name = def.Name + "-fitted",
                SourceFormat = "glb",
                SourceHash = ArchivePathRulesSha256(bytes),
                ValidationState = "fitted"
            };
            mesh.CanonicalGlbPath = $"assets/{mesh.MeshAssetId:D}/mesh.glb";
            _bundle.MeshBytes[mesh.MeshAssetId] = bytes;
            _bundle.Meshes.Add(mesh);
            _bundle.Project.AssetIds.Add(mesh.MeshAssetId);
            _bundle.GarmentDefinitions.Add(def);

            var instance = new GarmentInstance
            {
                DefinitionId = def.GarmentDefinitionId,
                CharacterId = character.CharacterId,
                MeshAssetId = mesh.MeshAssetId,
                FitState = "fitted",
                CollisionState = report is null ? "unknown" : $"insideAfter={report.InsideAfter}"
            };
            _bundle.GarmentInstances.Add(instance);
            character.GarmentInstanceIds.Add(instance.GarmentInstanceId);
            character.EditRevision++;
            Touch(character);
            _dirty = true;
        }
        RaiseChanged();
    }

    public void SetActiveRigFromGlb(string riggedGlbPath, string backendId)
    {
        SetActiveMeshFromGlbFile(riggedGlbPath, name: "rigged-body", source: SourceRepresentation.GeneratedMesh);
        lock (_gate)
        {
            var character = RequireActiveCharacter();
            var rig = new RigDefinition
            {
                Name = backendId,
                IsHumanoid = true,
                GeneratorProvenance = backendId
            };
            _bundle.Rigs.Add(rig);
            character.RigId = rig.RigId;
            character.EditRevision++;
            Touch(character);
            _dirty = true;
        }
        RaiseChanged();
    }

    public void AttachReferenceImage(ReferenceImage image, byte[] pngBytes)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(pngBytes);
        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(image.RelativePath))
                image.RelativePath = $"references/{image.ReferenceImageId:D}/image.png";
            _bundle.ReferenceImages.RemoveAll(r => r.ReferenceImageId == image.ReferenceImageId);
            _bundle.ReferenceImages.Add(image);
            _bundle.ReferenceImageBytes[image.ReferenceImageId] = pngBytes.ToArray();
            _dirty = true;
        }
        RaiseChanged();
    }

    private CharacterDocument RequireActiveCharacter()
    {
        var character = ActiveCharacterUnlocked();
        if (character is null)
            throw new InvalidOperationException("No active character in project session.");
        return character;
    }

    private CharacterDocument? ActiveCharacterUnlocked()
    {
        if (_activeCharacterId is not Guid id)
            return null;
        return _bundle.Characters.FirstOrDefault(c => c.CharacterId == id);
    }

    private SourceRepresentation InferSource()
    {
        var c = ActiveCharacterUnlocked();
        if (c?.ParametricHumanState is not null)
            return SourceRepresentation.AnnyParameters;
        return SourceRepresentation.GeneratedMesh;
    }

    private void RaiseChanged() => Changed?.Invoke();

    private static ProjectBundle CreateEmpty(string name)
    {
        var character = NewHumanCharacter(name);
        return new ProjectBundle
        {
            Project = new ProjectDocument
            {
                Name = name,
                CharacterIds = [character.CharacterId],
                AppVersionCreated = "2.0.0",
                AppVersionLastSaved = "2.0.0"
            },
            Characters = [character]
        };
    }

    private static CharacterDocument NewHumanCharacter(string name) =>
        new()
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Human" : name,
            CharacterKind = CharacterKind.ParametricHuman,
            SourceRepresentation = SourceRepresentation.AnnyParameters,
            ParametricHumanState = new ParametricHumanState
            {
                BackendId = "anny",
                TopologyProfile = "anny",
                RigProfile = "anny"
            }
        };

    private static void Touch(DomainDocument doc)
    {
        doc.ModifiedUtc = DateTime.UtcNow;
    }

    private static ProjectBundle Clone(ProjectBundle bundle) =>
        DomainJson.Deserialize<ProjectBundle>(DomainJson.Serialize(bundle));

    private static ParametricHumanState CloneState(ParametricHumanState source) =>
        DomainJson.Deserialize<ParametricHumanState>(DomainJson.Serialize(source));

    private static string ArchivePathRulesSha256(byte[] bytes)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
