namespace ThreeDGod.Core.Domain;

public sealed class AnnyHumanPreset
{
    public string Name { get; set; } = "";
    public string Backend { get; set; } = "anny";
    public Dictionary<string, float> Phenotypes { get; set; } = [];
    public Dictionary<string, float> LocalChanges { get; set; } = [];
    public Dictionary<string, float> FacialActions { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string? ThumbnailPath { get; set; }
    public bool BuiltIn { get; set; }
}

public static class AnnyPresetMapper
{
    public static ParametricHumanState ToState(AnnyHumanPreset preset) =>
        new()
        {
            BackendId = preset.Backend,
            TopologyProfile = "anny",
            RigProfile = "anny",
            SourcePreset = preset.Name,
            PhenotypeParameters = new Dictionary<string, float>(preset.Phenotypes),
            LocalShapeParameters = new Dictionary<string, float>(preset.LocalChanges),
            FacialActionParameters = new Dictionary<string, float>(preset.FacialActions)
        };

    public static AnnyHumanPreset FromState(string name, ParametricHumanState state, IEnumerable<string>? tags = null) =>
        new()
        {
            Name = name,
            Backend = string.IsNullOrWhiteSpace(state.BackendId) ? "anny" : state.BackendId,
            Phenotypes = new Dictionary<string, float>(state.PhenotypeParameters),
            LocalChanges = new Dictionary<string, float>(state.LocalShapeParameters),
            FacialActions = new Dictionary<string, float>(state.FacialActionParameters),
            Tags = tags?.ToList() ?? []
        };
}
