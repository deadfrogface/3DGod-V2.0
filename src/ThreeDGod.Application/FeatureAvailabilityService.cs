namespace ThreeDGod.Application;

/// <summary>
/// Honest feature gate. Placeholder/stub product paths are NotImplemented and not invocable.
/// Experimental paths may run but must not be presented as finished product.
/// </summary>
public sealed class FeatureAvailabilityService : IFeatureAvailabilityService
{
    public FeatureAvailability GetStatus(string featureId) => featureId switch
    {
        FeatureIds.PresetSave => FeatureAvailability.Available,
        FeatureIds.ViewportGlb => FeatureAvailability.Available,
        FeatureIds.ExportFbx => FeatureAvailability.Experimental,
        FeatureIds.ClothingFit => FeatureAvailability.NotImplemented,
        FeatureIds.PhysicsSimulate => FeatureAvailability.NotImplemented,
        FeatureIds.RigAuto => FeatureAvailability.NotImplemented,
        FeatureIds.ExportMetahuman => FeatureAvailability.NotImplemented,
        FeatureIds.AiGeneratePerson => FeatureAvailability.NotImplemented,
        FeatureIds.AiGenerateAsset => FeatureAvailability.NotImplemented,
        FeatureIds.ControllerInput => FeatureAvailability.NotImplemented,
        FeatureIds.ExportUnreal => FeatureAvailability.NotImplemented,
        FeatureIds.HeightMorph => FeatureAvailability.NotImplemented,
        FeatureIds.ConfigSave => FeatureAvailability.Available,
        _ => FeatureAvailability.NotImplemented
    };

    public bool IsInvocable(string featureId)
    {
        var status = GetStatus(featureId);
        return status is FeatureAvailability.Available or FeatureAvailability.Experimental;
    }

    public string GetStatusMessage(string featureId)
    {
        return featureId switch
        {
            FeatureIds.PresetSave => "Available – Preset-JSON wird auf die Platte geschrieben.",
            FeatureIds.ViewportGlb => "Available – vorhandene Base-GLBs werden im Viewport geladen.",
            FeatureIds.ExportFbx =>
                "Experimental – headless Blender-Job, falls Runtime da ist. Exportiert die Blender-Szene, nicht zwingend das Viewport-GLB. Datei wird nicht als fertiges Produkt bestätigt.",
            FeatureIds.ExportUnreal =>
                "NotImplemented – kein UE5-Pipeline-Export, nur Dateikopie wäre möglich. Button bleibt deaktiviert.",
            FeatureIds.ClothingFit =>
                "NotImplemented – kein Fitting, kein Mesh, keine Skinning-Pipeline.",
            FeatureIds.PhysicsSimulate =>
                "NotImplemented – keine Simulation. Flags werden nicht als Physik verkauft.",
            FeatureIds.RigAuto =>
                "NotImplemented – kein Auto-Rig, kein Skeleton-Output.",
            FeatureIds.ExportMetahuman =>
                "NotImplemented – kein MetaHuman-/UE-Mannequin-Workflow.",
            FeatureIds.AiGeneratePerson =>
                "NotImplemented – kein Image-to-3D, kein Mesh.",
            FeatureIds.AiGenerateAsset =>
                "NotImplemented – keine Asset-Generierung.",
            FeatureIds.ControllerInput =>
                "NotImplemented – Checkbox steuert kein Gamepad.",
            FeatureIds.HeightMorph =>
                "NotImplemented – Height is uniform scale, not anatomical morphing.",
            FeatureIds.ConfigSave => "Available – Config-JSON wird gespeichert.",
            _ => GetStatus(featureId) switch
            {
                FeatureAvailability.Available => "Available.",
                FeatureAvailability.NotInstalled => "NotInstalled – Backend oder Modell fehlt.",
                FeatureAvailability.Experimental => "Experimental – nicht für Production.",
                FeatureAvailability.UnsupportedHardware => "UnsupportedHardware – Hardware reicht nicht.",
                FeatureAvailability.Disabled => "Disabled.",
                FeatureAvailability.NotImplemented =>
                    "NotImplemented – kein echter End-to-End-Pfad.",
                _ => GetStatus(featureId).ToString()
            }
        };
    }
}
