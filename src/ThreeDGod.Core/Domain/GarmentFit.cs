namespace ThreeDGod.Core.Domain;

public sealed class BodyMeasurements
{
    public string SourceGlb { get; set; } = "";
    public float HeightM { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }
    public float ChestWidthM { get; set; }
    public float WaistWidthM { get; set; }
    public float ShoulderWidthM { get; set; }
    public float TorsoCenterX { get; set; }
    public float TorsoCenterY { get; set; }
    public float TorsoCenterZ { get; set; }
}

public sealed class ClippingRegion
{
    public string Name { get; set; } = "";
    public int InsideCount { get; set; }
    public float MinDistance { get; set; }
}

public sealed class ClippingReport
{
    public string PresetName { get; set; } = "";
    public string BodyGlb { get; set; } = "";
    public string GarmentGlb { get; set; } = "";
    public string FittedGlb { get; set; } = "";
    public string Backend { get; set; } = "geometry3Sharp";
    public BodyMeasurements Measurements { get; set; } = new();
    public int SampleCount { get; set; }
    public int InsideBefore { get; set; }
    public int InsideAfter { get; set; }
    public float MinDistanceAfter { get; set; }
    public float InflateMeters { get; set; }
    public List<ClippingRegion> Regions { get; set; } = [];
}

public sealed class GarmentFitResult
{
    public string FittedGlb { get; set; } = "";
    public string ReportPath { get; set; } = "";
    public ClippingReport Report { get; set; } = new();
}
