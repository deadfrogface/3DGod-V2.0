namespace ThreeDGod.Application;

/// <summary>
/// Capability contracts. Implementations are registered only when a real backend exists.
/// </summary>
public interface IProjectService;

public interface IWorkerHost;

public interface IBackendRegistry;

public interface IImportService;

public interface IExportService;

public interface IRiggingService;

public interface IImageTo3DService;

public interface ICharacterModelService
{
    void SetGender(string gender);
    void UpdateSculptValue(string key, int value);
    void LoadBaseModel(string gender);
    void SavePreset(string name = "default");
    void LoadPreset(string name = "default");
}
