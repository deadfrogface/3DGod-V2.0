using System.Numerics;
using System.Text.RegularExpressions;
using SharpGLTF.Schema2;
using ThreeDGod.Core.Diagnostics;
using ThreeDGod.Mesh;
using ThreeDGod.Rigging;

namespace ThreeDGod.Export;

public enum Ue5PreflightSeverity
{
    Hard,
    Soft
}

public sealed class Ue5PreflightIssue
{
    public Ue5PreflightSeverity Severity { get; init; }
    public string Category { get; init; } = "";
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed class Ue5PreflightReport
{
    public string ProfileId { get; init; } = UnrealEngine5ExportProfile.ProfileId;
    public string AssetPath { get; init; } = "";
    public bool Passed => !Issues.Any(i => i.Severity == Ue5PreflightSeverity.Hard);
    public List<Ue5PreflightIssue> Issues { get; } = [];
    public IReadOnlyList<string> HardMessages =>
        Issues.Where(i => i.Severity == Ue5PreflightSeverity.Hard).Select(i => $"[{i.Category}] {i.Message}").ToArray();
    public IReadOnlyList<string> SoftMessages =>
        Issues.Where(i => i.Severity == Ue5PreflightSeverity.Soft).Select(i => $"[{i.Category}] {i.Message}").ToArray();
}

/// <summary>
/// Preflight checks for Unreal Engine 5 skeletal/static mesh packaging.
/// Does NOT claim a successful UE5 editor import — only blocks clearly broken assets.
/// </summary>
public static class UnrealEngine5ExportProfile
{
    public const string ProfileId = "ue5-skeletal-mesh";
    public const float ExpectedUnitScaleMeters = 0.01f; // UE uses centimeters; GLB is meters → character height ~1–2.5m

    private static readonly Regex ValidAssetName = new(@"^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);
    private static readonly string[] CreatureBoneHints =
        ["tail", "wing", "horn", "ear.extra", "tentacle", "claw"];

    public static Ue5PreflightReport EvaluateGlb(
        string glbPath,
        string? assetName = null,
        bool requireSkin = true,
        IDiagnosticService? diagnostics = null)
    {
        return PipelineTrace.Run(diagnostics, "Export", "Export.Preflight", () =>
        {
            var report = new Ue5PreflightReport { AssetPath = glbPath ?? "" };
            if (string.IsNullOrWhiteSpace(glbPath) || !File.Exists(glbPath))
            {
                Hard(report, "file", "MissingAsset", "GLB missing – cannot preflight for UE5.");
                return report;
            }

            var info = new FileInfo(glbPath);
            if (info.Length < 64)
            {
                Hard(report, "file", "TooSmall", "GLB too small to be a valid scene.");
                return report;
            }

            CanonicalGltfDocument doc;
            try
            {
                doc = CanonicalGltfPipeline.Load(glbPath);
            }
            catch (Exception ex)
            {
                Hard(report, "file", "Unreadable", "GLB failed to load: " + ex.Message);
                return report;
            }

            CheckScale(report, glbPath);
            CheckSkeletonAndWeights(report, glbPath, requireSkin);
            CheckMorph(report, doc);
            CheckLod(report, doc);
            CheckMaterialsAndTextures(report, doc);
            CheckNaming(report, assetName ?? Path.GetFileNameWithoutExtension(glbPath));
            CheckExtraCreatureBones(report, glbPath);

            Soft(report, "ue5", "ImportNotClaimed",
                "Preflight only – real UE5 editor import is not claimed by this check.");
            return report;
        }, provider: ProfileId);
    }

    private static void CheckScale(Ue5PreflightReport report, string glbPath)
    {
        try
        {
            var (positions, _) = MeshCompare.ReadMesh(glbPath);
            if (positions.Count == 0)
            {
                Hard(report, "cm scale", "NoPositions", "Mesh has no positions for scale check.");
                return;
            }

            var min = positions[0];
            var max = positions[0];
            foreach (var p in positions)
            {
                if (!IsFinite(p))
                {
                    Hard(report, "cm scale", "NonFinitePosition", "Vertex position is NaN/Inf – UE5 would reject.");
                    return;
                }
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            var size = max - min;
            var height = MathF.Max(size.Y, MathF.Max(size.X, size.Z));
            // Expect meters (humanoid ~0.5–3m). If height looks like already-cm (>50), soft warn.
            if (height > 50f)
                Soft(report, "cm scale", "LikelyAlreadyCentimeters",
                    $"Bounding height {height:F1} suggests centimeters already; UE import may double-scale.");
            else if (height < 0.05f)
                Soft(report, "cm scale", "TinyMesh",
                    $"Bounding height {height:F3}m is tiny; check unit scale before UE import.");
            else if (height is >= 0.4f and <= 3.5f)
                Soft(report, "cm scale", "MetersOk",
                    $"Height {height:F2}m looks like meters; convert ×100 to cm for UE5.");
        }
        catch (Exception ex)
        {
            Soft(report, "cm scale", "ScaleCheckFailed", ex.Message);
        }
    }

    private static void CheckSkeletonAndWeights(Ue5PreflightReport report, string glbPath, bool requireSkin)
    {
        if (!requireSkin)
        {
            Soft(report, "skeleton", "SkinOptional", "Static mesh path – skin not required.");
            return;
        }

        var rig = RigValidator.ValidateGlb(glbPath, requireHumanoid: false);
        if (rig.SkinCount == 0)
            Hard(report, "skeleton", "NoSkin", "No skin/skeleton – UE5 skeletal mesh export blocked.");
        foreach (var f in rig.Failures)
        {
            var cat = f.Code is "NoSkinAttributes" or "BadWeightSum" or "BadJointIndex" ? "weights" : "skeleton";
            Hard(report, cat, f.Code, f.Message);
        }
    }

    private static void CheckMorph(Ue5PreflightReport report, CanonicalGltfDocument doc)
    {
        if (doc.MorphTargetCount == 0)
            Soft(report, "morph", "NoMorph", "No morph targets – OK if unused in UE.");
        else
            Soft(report, "morph", "MorphPresent", $"{doc.MorphTargetCount} morph target(s) present.");
    }

    private static void CheckLod(Ue5PreflightReport report, CanonicalGltfDocument doc)
    {
        if (doc.MeshCount <= 1)
            Soft(report, "LOD", "SingleLod", "Only one mesh LOD – generate LODs in-engine or via remesh before shipping.");
        else
            Soft(report, "LOD", "MultiMesh", $"{doc.MeshCount} meshes – treat as LOD candidates only if authored as such.");
    }

    private static void CheckMaterialsAndTextures(Ue5PreflightReport report, CanonicalGltfDocument doc)
    {
        if (doc.MaterialCount == 0)
            Hard(report, "materials", "NoMaterials", "No materials – UE import would be incomplete.");
        if (doc.ImageCount == 0 && doc.TextureCount == 0)
            Soft(report, "textures", "NoTextures", "No embedded textures – base color may be factor-only.");
        if (!doc.HasUv)
            Soft(report, "textures", "NoUv", "No TEXCOORD_0 – textured materials will not map correctly.");
    }

    private static void CheckNaming(Ue5PreflightReport report, string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
        {
            Hard(report, "naming", "EmptyName", "Asset name is empty.");
            return;
        }

        if (!ValidAssetName.IsMatch(assetName))
            Hard(report, "naming", "InvalidName",
                $"Asset name '{assetName}' is not UE-safe (use letters/digits/underscore, start with letter).");
    }

    private static void CheckExtraCreatureBones(Ue5PreflightReport report, string glbPath)
    {
        try
        {
            var model = ModelRoot.Load(glbPath);
            var names = model.LogicalNodes.Select(n => n.Name ?? "").Where(n => n.Length > 0).ToArray();
            var extras = names.Where(n =>
                CreatureBoneHints.Any(h => n.Contains(h, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (extras.Length > 0)
                Soft(report, "extra creature bones", "CreatureBones",
                    "Extra creature bones detected: " + string.Join(", ", extras) +
                    " – map via custom UE skeleton profile (not Mannequin-only).");
        }
        catch
        {
            // already handled by load failures elsewhere
        }
    }

    private static void Hard(Ue5PreflightReport report, string category, string code, string message) =>
        report.Issues.Add(new Ue5PreflightIssue
        {
            Severity = Ue5PreflightSeverity.Hard,
            Category = category,
            Code = code,
            Message = message
        });

    private static void Soft(Ue5PreflightReport report, string category, string code, string message) =>
        report.Issues.Add(new Ue5PreflightIssue
        {
            Severity = Ue5PreflightSeverity.Soft,
            Category = category,
            Code = code,
            Message = message
        });

    private static bool IsFinite(Vector3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
