using ThreeDGodCreator.Core.Models;

namespace ThreeDGodCreator.Core.Services;

/// <summary>
/// Deterministic Character Creator state. UI sliders and AI edit plans must both call this layer,
/// so AI never becomes a second, incompatible character-editing system.
/// </summary>
public sealed class CharacterCreatorService
{
    private readonly Dictionary<string, CharacterCreatorDefinition> _presets =
        new(StringComparer.OrdinalIgnoreCase);

    public CharacterCreatorService()
    {
        RegisterPreset("human", new CharacterCreatorDefinition
        {
            BaseKind = CharacterBaseKind.Human,
            BaseProvider = "anny"
        });

        var orc = new CharacterCreatorDefinition
        {
            BaseKind = CharacterBaseKind.HumanoidCreature,
            BaseProvider = "anny",
            Species = "orc"
        };
        orc.Morphs["muscle"] = 0.70;
        orc.Morphs["shoulder_width"] = 0.45;
        orc.Morphs["jaw_width"] = 0.55;
        orc.Morphs["brow_ridge"] = 0.55;
        orc.Morphs["nose_width"] = 0.30;
        orc.Appearance["skin_tone"] = "orc_grey_green";
        orc.Parts["tusk_left"] = new CharacterPartDefinition
        {
            Id = "tusk_left", Kind = CharacterPartKind.CreaturePart, AssetId = "tusk_basic"
        };
        orc.Parts["tusk_right"] = new CharacterPartDefinition
        {
            Id = "tusk_right", Kind = CharacterPartKind.CreaturePart, AssetId = "tusk_basic"
        };
        RegisterPreset("orc", orc);
    }

    public IReadOnlyCollection<string> Presets => _presets.Keys;

    public void RegisterPreset(string id, CharacterCreatorDefinition definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(definition);
        _presets[id] = definition.Clone();
    }

    public CharacterCreatorDefinition Create(string presetId)
    {
        if (!_presets.TryGetValue(presetId, out var preset))
            throw new KeyNotFoundException($"Unknown character preset '{presetId}'.");
        return preset.Clone();
    }

    public void SetMorph(CharacterCreatorDefinition character, string morphId, double value)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentException.ThrowIfNullOrWhiteSpace(morphId);
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), "Morph value must be finite.");
        character.Morphs[morphId] = Math.Clamp(value, -1.0, 1.0);
    }

    public void AddMorphDelta(CharacterCreatorDefinition character, string morphId, double delta)
    {
        var current = character.Morphs.TryGetValue(morphId, out var value) ? value : 0.0;
        SetMorph(character, morphId, current + delta);
    }

    public void SetAppearance(CharacterCreatorDefinition character, string property, string value)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        character.Appearance[property] = value;
    }

    public void SetPart(CharacterCreatorDefinition character, string slot, CharacterPartKind kind, string? assetId)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentException.ThrowIfNullOrWhiteSpace(slot);

        if (string.IsNullOrWhiteSpace(assetId))
        {
            character.Parts.Remove(slot);
            return;
        }

        character.Parts[slot] = new CharacterPartDefinition
        {
            Id = slot,
            Kind = kind,
            AssetId = assetId,
            Enabled = true
        };
    }
}
