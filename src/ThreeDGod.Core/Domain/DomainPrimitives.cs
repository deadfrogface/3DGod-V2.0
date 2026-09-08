namespace ThreeDGod.Core.Domain;

/// <summary>
/// Tool-agnostic 3D transform. Must never reference WPF or Helix types.
/// </summary>
public sealed class SpatialTransform
{
    public double Tx { get; set; }
    public double Ty { get; set; }
    public double Tz { get; set; }
    public double Qx { get; set; }
    public double Qy { get; set; }
    public double Qz { get; set; }
    public double Qw { get; set; } = 1;
    public double Sx { get; set; } = 1;
    public double Sy { get; set; } = 1;
    public double Sz { get; set; } = 1;
}

public sealed class AxisAlignedBounds
{
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MinZ { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
    public double MaxZ { get; set; }
}

public sealed class ColorRgba
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; } = 1f;
}

public sealed class TextureTransform
{
    public double OffsetU { get; set; }
    public double OffsetV { get; set; }
    public double ScaleU { get; set; } = 1;
    public double ScaleV { get; set; } = 1;
    public double Rotation { get; set; }
}

public abstract class DomainDocument
{
    public int SchemaVersion { get; set; } = 1;
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }

    protected DomainDocument()
    {
        var now = DateTime.UtcNow;
        CreatedUtc = now;
        ModifiedUtc = now;
    }
}
