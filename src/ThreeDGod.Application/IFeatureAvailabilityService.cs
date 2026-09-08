namespace ThreeDGod.Application;

public enum FeatureAvailability
{
    Available,
    NotInstalled,
    Experimental,
    UnsupportedHardware,
    Disabled,
    NotImplemented
}

public static class FeatureIds
{
    public const string ClothingFit = "clothing.fit";
    public const string PhysicsSimulate = "physics.simulate";
    public const string RigAuto = "rig.auto";
    public const string ExportMetahuman = "export.metahuman";
    public const string AiGeneratePerson = "ai.generate.person";
    public const string AiGenerateAsset = "ai.generate.asset";
    public const string ControllerInput = "input.controller";
    public const string ExportFbx = "export.fbx";
    public const string ExportUnreal = "export.unreal";
    public const string PresetSave = "preset.save";
    public const string ViewportGlb = "viewport.glb";
    public const string HeightMorph = "character.height.morph";
    public const string ConfigSave = "config.save";
    public const string AnnyHuman = "human.anny";
    public const string ExportGlb = "export.glb";
    public const string ProjectSave = "project.save";
    public const string ReferenceImageGenerate = "image.reference.generate";
    public const string ImageTo3D = "image.to3d";
    public const string Remesh = "mesh.remesh";
    public const string SkinTokens = "rig.skintokens";
}

public interface IFeatureAvailabilityService
{
    FeatureAvailability GetStatus(string featureId);
    bool IsInvocable(string featureId);
    string GetStatusMessage(string featureId);
}
