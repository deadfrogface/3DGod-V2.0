using ThreeDGod.Application;
using ThreeDGod.Workers;

namespace ThreeDGod.Infrastructure;

public sealed class DynamicFeatureAvailabilityService : IFeatureAvailabilityService
{
    private readonly FeatureAvailabilityService _inner = new();

    public FeatureAvailability GetStatus(string featureId)
    {
        if (featureId == FeatureIds.AnnyHuman)
            return AnnyRuntime.Probe().Availability;
        if (featureId == FeatureIds.ExportGlb || featureId == FeatureIds.ProjectSave)
            return FeatureAvailability.Available;
        if (featureId == FeatureIds.ReferenceImageGenerate)
            return ReferenceImageRuntime.Probe().Availability;
        if (featureId == FeatureIds.ImageTo3D)
            return ImageTo3DRuntime.Probe("triposr").Availability;
        if (featureId == FeatureIds.AiGenerateAsset)
            return FeatureAvailability.Experimental;
        if (featureId == FeatureIds.Remesh)
            return FeatureAvailability.Available;
        if (featureId == FeatureIds.SkinTokens)
            return SkinTokensRuntime.Probe().Availability;
        if (featureId == FeatureIds.RigValidate)
            return FeatureAvailability.Available;
        if (featureId == FeatureIds.CreatureParts)
            return FeatureAvailability.Available;
        return _inner.GetStatus(featureId);
    }

    public bool IsInvocable(string featureId)
    {
        var status = GetStatus(featureId);
        return status is FeatureAvailability.Available or FeatureAvailability.Experimental;
    }

    public string GetStatusMessage(string featureId)
    {
        if (featureId == FeatureIds.AnnyHuman)
            return AnnyRuntime.Probe().Message;
        if (featureId == FeatureIds.ExportGlb)
            return "Available – GLB export copies a verified source mesh.";
        if (featureId == FeatureIds.ProjectSave)
            return "Available – .3dgod ZIP save/load.";
        if (featureId == FeatureIds.ReferenceImageGenerate)
            return ReferenceImageRuntime.Probe().Message;
        if (featureId == FeatureIds.ImageTo3D)
            return ImageTo3DRuntime.Probe("triposr").Message;
        if (featureId == FeatureIds.AiGenerateAsset)
            return "Experimental – procedural catalog assets (jewelry). FLUX/TripoSR remain NotInstalled.";
        if (featureId == FeatureIds.Remesh)
            return "Available – in-process vertex-cluster remesh + spherical UVs. Not instant-meshes / xatlas.";
        if (featureId == FeatureIds.SkinTokens)
            return SkinTokensRuntime.Probe().Message;
        if (featureId == FeatureIds.RigValidate)
            return "Available – hierarchy/weight/bind validator and linear-blend test poses.";
        if (featureId == FeatureIds.CreatureParts)
            return "Available – modular extra parts (tail/horns) persist in .3dgod.";
        return _inner.GetStatusMessage(featureId);
    }
}
