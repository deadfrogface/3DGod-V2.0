using ThreeDGod.Core.Domain;
using ThreeDGod.Core.Editing;

namespace ThreeDGod.Application;

/// <summary>
/// Capability contracts. Implementations are registered only when a real backend exists.
/// </summary>
public interface IProjectService
{
    Task SaveAsync(ProjectBundle bundle, string destinationPath, CancellationToken cancellationToken = default);
    Task<ProjectBundle> LoadAsync(string sourcePath, CancellationToken cancellationToken = default);
}

public sealed record WorkerRequest(string Method, string JsonParams, string? JobId = null, string? BackendId = null);

public sealed record WorkerRunResult(
    bool Ok,
    string? JsonPayload,
    string? ErrorCode,
    string? ErrorMessage,
    int? ExitCode,
    bool Crashed,
    bool TimedOut,
    bool Cancelled);

public interface IWorkerHost
{
    Task<WorkerRunResult> RunAsync(string executable, IReadOnlyList<string> arguments, WorkerRequest request, TimeSpan timeout, CancellationToken cancellationToken = default);
}

/// <summary>FBX/Assimp import — registered only when a real backend is installed.</summary>
public interface IImportService
{
    FeatureAvailability Probe();
    string ProbeMessage();
}

/// <summary>Legacy catch-all export marker; prefer <see cref="IGlbExportService"/> / <see cref="IFbxExportService"/>.</summary>
public interface IExportService
{
    FeatureAvailability Probe();
    string ProbeMessage();
}

/// <summary>Auto-rig / SkinTokens style backends. Authored + freeform distance weights stay available separately.</summary>
public interface IRiggingService
{
    FeatureAvailability Probe();
    string ProbeMessage();
}

public interface IAutoRigBackend
{
    string BackendId { get; }
    FeatureAvailability Probe();
    string ProbeMessage();
}

public interface IImageTo3DService
{
    FeatureAvailability Probe(string backendId = "triposr");
    string ProbeMessage(string backendId = "triposr");
    Task<string> GenerateGlbAsync(string imagePath, string destinationGlb, string backendId = "triposr", CancellationToken cancellationToken = default);
}

public interface IAssetGenerationService
{
    Task<LibraryAsset> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}

public interface IRemeshService
{
    string RemeshGlb(string sourceGlb, string destinationGlb, RemeshProfile profile);
}

public interface ISkinTokensRigService
{
    FeatureAvailability Probe();
    string ProbeMessage();
    Task<string> RigGlbAsync(string sourceGlb, string destinationGlb, CancellationToken cancellationToken = default);
}

public interface IRigValidator
{
    bool ValidateGlb(string glbPath, out IReadOnlyList<string> failures, bool requireHumanoid = true);
}

public interface ICreatureAssembly
{
    CharacterDocument AttachHumanTailAndHorns(ProjectBundle bundle, string meshRoot);
    CharacterDocument CreateOrc(ProjectBundle bundle, string meshRoot);
    CharacterDocument CreateRat(ProjectBundle bundle, string meshRoot);
    CharacterDocument CreateEditableHumanoid(ProjectBundle bundle, string meshRoot);
}

public interface ICreatureTextEditService
{
    Task ApplyAsync(string prompt, ProjectBundle bundle, CharacterDocument character, string meshRoot, CommandStack stack, CancellationToken cancellationToken = default);
}

public interface IFreeformCharacterPipeline
{
    Task<CharacterDocument> RunAsync(string prompt, ProjectBundle bundle, string workRoot, CancellationToken cancellationToken = default);
}

public interface IGarmentService
{
    GarmentDefinition GetTemplate(string name);
    GarmentInstance Instantiate(GarmentDefinition definition, Guid characterId);
    string BuildMesh(GarmentDefinition definition, GarmentInstance instance, string destinationGlb);
    Task ApplyTextAsync(GarmentDefinition definition, GarmentInstance instance, string prompt, CommandStack stack, CancellationToken cancellationToken = default);
}

public interface IGarmentCodeService
{
    FeatureAvailability Probe();
    string ProbeMessage();
    Task<string> GenerateJacketGlbAsync(string destinationGlb, CancellationToken cancellationToken = default);
}

public interface IGarmentFitService
{
    Task<GarmentFitResult> FitJacketToBodyAsync(string bodyGlb, string workRoot, string presetName, CancellationToken cancellationToken = default);
    Task<GarmentFitResult> FitJacketToPresetAsync(string presetName, string workRoot, CancellationToken cancellationToken = default);
}

public interface IGarmentSkinService
{
    string BindToBody(string skinnedBodyGlb, string garmentGlb, string destinationGlb);
}

public interface IReferenceImageGenerationService
{
    FeatureAvailability Probe();
    string ProbeMessage();
    Task<ReferenceImage> GenerateAsync(string prompt, long? seed, ProjectBundle bundle, CancellationToken cancellationToken = default);
    ReferenceImage AttachExistingPng(string pngPath, string prompt, long? seed, ProjectBundle bundle);
}

public interface ICharacterModelService
{
    void SetGender(string gender);
    void UpdateSculptValue(string key, int value);
    void LoadBaseModel(string gender);
    void SavePreset(string name = "default");
    void LoadPreset(string name = "default");
}

public sealed class PhysicsBodySample
{
    public string Name { get; init; } = "";
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public float Speed { get; init; }
}

public sealed class PhysicsSnapshot
{
    public IReadOnlyList<PhysicsBodySample> Bodies { get; init; } = [];
    public float KineticEnergy { get; init; }
}

public interface IAccessoryPhysicsPreview : IDisposable
{
    string Kind { get; }
    void Step(float dt);
    void Reset();
    PhysicsSnapshot Capture();
}

public interface IAccessoryPhysicsService
{
    IAccessoryPhysicsPreview CreateHangingChain();
    IAccessoryPhysicsPreview CreateEarringAgainstHead();
}

public sealed class GlbSceneCounts
{
    public int MeshCount { get; init; }
    public int MaterialCount { get; init; }
    public int TextureCount { get; init; }
    public int ImageCount { get; init; }
    public int SkinCount { get; init; }
    public int MorphTargetCount { get; init; }
    public int NodeCount { get; init; }
    public int VertexCount { get; init; }
    public int TriangleCount { get; init; }
    public bool HasUv { get; init; }
    public bool HasJoints { get; init; }
}

public interface IGlbExportService
{
    string WriteCompleteScene(string destinationGlb);
    string Export(string sourceGlb, string destinationGlb);
    GlbSceneCounts Inspect(string glbPath);
}

public interface IFbxExportService
{
    /// <summary>Viewport GLB → FBX via headless Blender. Does not claim UE5 editor import.</summary>
    string Export(string sourceGlb, string destinationFbx, string? assetName = null, bool runUe5Preflight = true);
}
