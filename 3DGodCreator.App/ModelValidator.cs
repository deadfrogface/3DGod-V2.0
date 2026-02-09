using SharpGLTF.Schema2;
using ThreeDGodCreator.Core.Services;

namespace ThreeDGodCreator.App;

/// <summary>
/// Validates GLB/glTF models for rig compatibility.
/// </summary>
public static class ModelValidator
{
    public record ValidationResult(
        bool IsValid,
        bool HasRig,
        bool HasSkin,
        int MeshCount,
        int NodeCount,
        string? Message
    );

    /// <summary>
    /// Validate a GLB file for rig and transform compatibility.
    /// </summary>
    public static ValidationResult Validate(string path)
    {
        try
        {
            var model = ModelRoot.Load(path);
            if (model == null)
                return new ValidationResult(false, false, false, 0, 0, "Could not load model.");

            var hasSkin = model.LogicalSkins != null && model.LogicalSkins.Count > 0;
            var nodeCount = model.LogicalNodes?.Count ?? 0;
            var meshCount = model.LogicalMeshes?.Count ?? 0;

            var hasRig = hasSkin || nodeCount > 1;

            string? msg = null;
            if (meshCount == 0)
                msg = "No meshes found. Model may be empty.";
            else if (!hasRig)
                msg = "Model has no armature/skin. Slider deformations may not work. Consider using a rigged base model.";

            AppLogger.Write($"[ModelValidator] {path}: meshes={meshCount}, nodes={nodeCount}, hasSkin={hasSkin}");

            return new ValidationResult(
                IsValid: meshCount > 0,
                HasRig: hasRig,
                HasSkin: hasSkin,
                MeshCount: meshCount,
                NodeCount: nodeCount,
                Message: msg
            );
        }
        catch (Exception ex)
        {
            AppLogger.LogException(ex, "ModelValidator.Validate");
            return new ValidationResult(false, false, false, 0, 0, ex.Message);
        }
    }
}
