namespace ThreeDGodCreator.Core.Models;

/// <summary>
/// Structured error info when Blender fails.
/// </summary>
public record BlenderErrorInfo(
    BlenderErrorCode Code,
    string Message,
    string? Detail = null,
    string? SuggestedFix = null
);

public enum BlenderErrorCode
{
    NotInstalled,
    PathInvalid,
    ProcessStartFailed,
    ProcessExitedUnexpectedly,
    PermissionDenied,
    ScriptNotFound,
    Timeout,
    Unknown
}
