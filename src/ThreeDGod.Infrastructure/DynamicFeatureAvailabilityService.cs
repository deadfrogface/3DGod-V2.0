using ThreeDGod.Application;
using ThreeDGod.Workers;
using ThreeDGod.AI;

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
        if (featureId == FeatureIds.CreatureTextEdit)
            return FeatureAvailability.Available;
        if (featureId == FeatureIds.FreeformPipeline)
            return FeatureAvailability.Experimental;
        if (featureId == FeatureIds.GarmentTemplates)
            return FeatureAvailability.Available;
        if (featureId == FeatureIds.GarmentCode)
            return GarmentCodeRuntime.Probe().Availability;
        if (featureId == FeatureIds.ClothingFit)
        {
            var gc = GarmentCodeRuntime.Probe().Availability;
            return gc is FeatureAvailability.Available or FeatureAvailability.Experimental
                ? FeatureAvailability.Experimental
                : gc;
        }
        if (featureId == FeatureIds.GarmentSkin)
            return FeatureAvailability.Experimental;
        if (featureId == FeatureIds.GenerativeGarment)
            return FeatureAvailability.NotImplemented;
        if (featureId == FeatureIds.LocalAiMeshEdit)
            return FeatureAvailability.NotImplemented;
        if (featureId == FeatureIds.PhysicsSimulate)
            return FeatureAvailability.Experimental;
        if (featureId == FeatureIds.AiCommandInterpret)
            return FeatureAvailability.Available;
        if (featureId == FeatureIds.AiLlamaSharp)
        {
            var llama = LlamaSharpProvider.Probe();
            return llama.Availability switch
            {
                FeatureAvailability.Experimental => FeatureAvailability.NotImplemented,
                FeatureAvailability.Available => FeatureAvailability.NotImplemented,
                _ => llama.Availability
            };
        }
        if (featureId == FeatureIds.MaterialEditorPbr)
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
        if (featureId == FeatureIds.CreatureTextEdit)
            return "Available – catalog ReplaceBodyPart / AddCreaturePart from text. Generated AI parts remain NotInstalled.";
        if (featureId == FeatureIds.FreeformPipeline)
            return "Experimental – catalog freeform (dragon) + remesh + authored rig. FLUX/TripoSR/SkinTokens remain NotInstalled.";
        if (featureId == FeatureIds.GarmentTemplates)
            return "Available – parametric T-Shirt/Jacket/Pants/Coat templates. Fitting remains NotImplemented.";
        if (featureId == FeatureIds.GarmentCode)
            return GarmentCodeRuntime.Probe().Message;
        if (featureId == FeatureIds.ClothingFit)
        {
            var gc = GarmentCodeRuntime.Probe();
            if (gc.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.UnsupportedHardware or FeatureAvailability.Disabled)
                return gc.Message;
            return "Experimental – geometry3Sharp proximity fit + inflate. Not cloth simulation.";
        }
        if (featureId == FeatureIds.GarmentSkin)
            return "Experimental – nearest-vertex weight transfer onto the body skeleton. Not SkinTokens.";
        if (featureId == FeatureIds.GenerativeGarment)
            return "NotImplemented – DressCode/GarmentDiffusion/Garment3DGen not product-cleared. See docs/research/GENERATIVE_GARMENT_DECISION.md. No button.";
        if (featureId == FeatureIds.LocalAiMeshEdit)
            return "NotImplemented – BlendedPC/StructLDM/GaussCtrl/TrAME have no Anny mesh PoC. Parameter + catalog replace only. See docs/research/LOCAL_AI_EDIT_DECISION.md. No button.";
        if (featureId == FeatureIds.PhysicsSimulate)
            return "Experimental – Bepu rigid accessory chain/earring preview. Not cloth, not softbody, not ragdoll.";
        if (featureId == FeatureIds.AiCommandInterpret)
            return "Available – deterministic allow-listed parser + validator. Not LLM inference.";
        if (featureId == FeatureIds.AiLlamaSharp)
        {
            var llama = LlamaSharpProvider.Probe();
            if (llama.Availability is FeatureAvailability.NotInstalled or FeatureAvailability.Disabled)
                return llama.Message;
            return "NotImplemented – LLamaSharp GGUF may be present but no verified prompt-to-plan inference. Deterministic parser only.";
        }
        if (featureId == FeatureIds.MaterialEditorPbr)
            return "Available – catalog PBR presets edit baseColor/metallic/roughness in viewport and GLB.";
        return _inner.GetStatusMessage(featureId);
    }
}
