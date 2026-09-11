using ThreeDGod.Core.Domain;

namespace ThreeDGod.Application;

/// <summary>
/// Maps product "height" intent onto Anny phenotype / local-change keys.
/// Never applies uniform scene scale — callers must regenerate the human mesh.
/// </summary>
public static class AnnyHeightMorph
{
    public const string ProductParameterKey = "height";

    /// <summary>
    /// Prefer explicit Anny catalog keys that describe body height / stature.
    /// Falls back to product key "height" when the catalog exposes it.
    /// </summary>
    public static string ResolvePhenotypeKey(IReadOnlyList<string> phenotypeKeys, IReadOnlyList<string>? localChangeKeys = null)
    {
        static string? Pick(IReadOnlyList<string>? keys, params string[] preferred)
        {
            if (keys is null || keys.Count == 0)
                return null;
            foreach (var p in preferred)
            {
                var hit = keys.FirstOrDefault(k => string.Equals(k, p, StringComparison.OrdinalIgnoreCase));
                if (hit is not null)
                    return hit;
            }

            foreach (var p in preferred)
            {
                var hit = keys.FirstOrDefault(k => k.Contains(p, StringComparison.OrdinalIgnoreCase));
                if (hit is not null)
                    return hit;
            }

            return null;
        }

        return Pick(phenotypeKeys, "height", "stature", "body_height", "tall")
               ?? Pick(localChangeKeys, "height", "torso_length", "leg_length")
               ?? ProductParameterKey;
    }

    public static void ApplyTaller(ParametricHumanState human, float delta, string resolvedKey, bool intoLocalChanges = false)
    {
        var map = intoLocalChanges ? human.LocalShapeParameters : human.PhenotypeParameters;
        map.TryGetValue(resolvedKey, out var old);
        map[resolvedKey] = Math.Clamp(old + delta, 0f, 1f);
        // Keep product-facing height in sync for UI/undo metadata.
        human.PhenotypeParameters.TryGetValue(ProductParameterKey, out var productOld);
        human.PhenotypeParameters[ProductParameterKey] = Math.Clamp(productOld + delta, 0f, 1f);
    }

    /// <summary>
    /// True when vertical bounds changed without a uniform XYZ scale of the whole mesh.
    /// </summary>
    public static bool LooksLikeNonUniformHeightChange(
        float beforeHeight,
        float afterHeight,
        float beforeWidth,
        float afterWidth,
        float minHeightRatio = 1.02f,
        float maxWidthRatio = 1.08f)
    {
        if (beforeHeight <= 1e-5f || beforeWidth <= 1e-5f)
            return false;
        var h = afterHeight / beforeHeight;
        var w = afterWidth / beforeWidth;
        return h >= minHeightRatio && w <= maxWidthRatio;
    }
}

