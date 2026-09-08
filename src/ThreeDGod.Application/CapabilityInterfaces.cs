using ThreeDGod.Core.Domain;

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

public interface IImportService;

public interface IExportService;

public interface IRiggingService;

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
