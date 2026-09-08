using ThreeDGodCreator.Core;

namespace ThreeDGod.Application;

/// <summary>
/// Adapter so the existing CharacterSystem can be resolved as ICharacterModelService
/// without inventing new behavior.
/// </summary>
public sealed class CharacterModelServiceAdapter : ICharacterModelService
{
    private readonly CharacterSystem _characterSystem;

    public CharacterModelServiceAdapter(CharacterSystem characterSystem)
    {
        _characterSystem = characterSystem;
    }

    public void SetGender(string gender) => _characterSystem.SetGender(gender);
    public void UpdateSculptValue(string key, int value) => _characterSystem.UpdateSculptValue(key, value);
    public void LoadBaseModel(string gender) => _characterSystem.LoadBaseModel(gender);
    public void SavePreset(string name = "default") => _characterSystem.SavePreset(name);
    public void LoadPreset(string name = "default") => _characterSystem.LoadPreset(name);
}
