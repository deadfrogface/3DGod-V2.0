using ThreeDGodCreator.Core.Models;

namespace ThreeDGodCreator.Core.Services;

/// <summary>
/// Process-boundary operations for the legacy Blender host.
/// Implementation lives in Infrastructure, not in Core.
/// </summary>
public interface IBlenderOperations
{
    string GetBlenderPath();
    string? DetectBlenderPath();
    bool VerifyCanLaunch(out string? errorMessage);
    void SendSculptData(Dictionary<string, object> sculptData);
    void LaunchSculpt();
    void LaunchAutoRig();
    void ExportFbx(string filename = "exported_character");
    bool IsBlenderConfigured();
    bool IsBlenderProcessRunning { get; }
    event Action<string>? OnLog;
    event Action? OnBlenderNotFound;
    event Action<BlenderErrorInfo>? OnBlenderFailed;
}
