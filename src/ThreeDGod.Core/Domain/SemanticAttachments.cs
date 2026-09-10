namespace ThreeDGod.Core.Domain;

public static class SemanticSlots
{
    public const string Neck = "neck";
    public const string Chest = "chest";
    public const string EarLeft = "ear.L";
    public const string EarRight = "ear.R";
    public const string Nose = "nose";
    public const string WristLeft = "wrist.L";
    public const string WristRight = "wrist.R";
    public const string HandLeft = "hand.L";
    public const string HandRight = "hand.R";
    public const string Hip = "hips";
}

public static class AttachmentDefaults
{
    public static string SlotFor(AttachmentType type) => type switch
    {
        AttachmentType.Necklace => SemanticSlots.Neck,
        AttachmentType.Earring => SemanticSlots.EarLeft,
        AttachmentType.Weapon => SemanticSlots.HandRight,
        AttachmentType.Horn => "head",
        AttachmentType.Ring => SemanticSlots.HandLeft,
        AttachmentType.Tail => "hips",
        AttachmentType.Hair => "head",
        AttachmentType.Beard => "head",
        AttachmentType.Piercing => SemanticSlots.Nose,
        AttachmentType.Prosthetic => SemanticSlots.WristRight,
        _ => SemanticSlots.Chest
    };
}

public static class AttachmentKinematics
{
    public static SpatialTransform Combine(SpatialTransform parent, SpatialTransform local) =>
        new()
        {
            Tx = parent.Tx + local.Tx,
            Ty = parent.Ty + local.Ty,
            Tz = parent.Tz + local.Tz,
            Qx = parent.Qx,
            Qy = parent.Qy,
            Qz = parent.Qz,
            Qw = parent.Qw,
            Sx = parent.Sx * local.Sx,
            Sy = parent.Sy * local.Sy,
            Sz = parent.Sz * local.Sz
        };

    public static SpatialTransform RotateY(SpatialTransform transform, double degrees)
    {
        var rad = degrees * Math.PI / 180.0;
        var c = Math.Cos(rad);
        var s = Math.Sin(rad);
        var x = transform.Tx * c - transform.Tz * s;
        var z = transform.Tx * s + transform.Tz * c;
        return new SpatialTransform
        {
            Tx = x,
            Ty = transform.Ty,
            Tz = z,
            Qy = s,
            Qw = c,
            Sx = transform.Sx,
            Sy = transform.Sy,
            Sz = transform.Sz
        };
    }

    public static AttachmentInstance Bind(Guid characterId, Guid assetId, AttachmentType type, SpatialTransform local) =>
        new()
        {
            CharacterId = characterId,
            AssetId = assetId,
            AttachmentType = type,
            ParentBoneSemantic = AttachmentDefaults.SlotFor(type),
            LocalTransform = local
        };

    public static SpatialTransform WorldOnPose(AttachmentInstance attachment, SpatialTransform parentPose) =>
        Combine(parentPose, attachment.LocalTransform);
}


/// <summary>
/// Shared socket offsets for attachments. No per-accessory pipeline — one kinematic binder.
/// </summary>
public static class AttachmentSockets
{
    public static SpatialTransform LocalOffset(AttachmentType type) => type switch
    {
        AttachmentType.Horn => new SpatialTransform { Ty = 0.12, Tz = 0.02 },
        AttachmentType.Tail => new SpatialTransform { Tz = -0.18 },
        AttachmentType.Necklace => new SpatialTransform { Ty = -0.02, Tz = 0.03 },
        AttachmentType.Earring => new SpatialTransform { Tx = 0.06, Ty = 0.02 },
        AttachmentType.Hair => new SpatialTransform { Ty = 0.08 },
        AttachmentType.Beard => new SpatialTransform { Ty = -0.06, Tz = 0.04 },
        AttachmentType.Piercing => new SpatialTransform { Tz = 0.05 },
        AttachmentType.Weapon => new SpatialTransform { Ty = -0.05, Tz = 0.02 },
        AttachmentType.Ring => new SpatialTransform { Ty = -0.01 },
        AttachmentType.Prosthetic => new SpatialTransform { Ty = -0.02 },
        _ => new SpatialTransform()
    };

    public static AttachmentInstance Attach(
        Guid characterId,
        Guid assetId,
        AttachmentType type,
        SpatialTransform? extraLocal = null)
    {
        var local = AttachmentKinematics.Combine(LocalOffset(type), extraLocal ?? new SpatialTransform());
        return AttachmentKinematics.Bind(characterId, assetId, type, local);
    }
}
