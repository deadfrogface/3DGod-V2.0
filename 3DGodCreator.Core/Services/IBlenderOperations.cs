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
    bool TryExportGlbToFbx(string sourceGlb, string destinationFbx, out string? error);
    bool IsBlenderConfigured();
    bool IsBlenderProcessRunning { get; }
    bool TryRunHeadlessJob(string pythonScriptPath, out string output, out string? error);
    bool TryRunHeadlessJob(string pythonScriptPath, IReadOnlyList<string>? scriptArgs, out string output, out string? error, int timeoutMs = 20000);
    event Action<string>? OnLog;
    event Action? OnBlenderNotFound;
    event Action<BlenderErrorInfo>? OnBlenderFailed;
}
