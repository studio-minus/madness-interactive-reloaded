using Walgelijk;

namespace MIR;

public class CharacterPresetComponent : Component
{
    public readonly ExperimentCharacterPreset Preset;

    public CharacterPresetComponent(ExperimentCharacterPreset preset)
    {
        Preset = preset;
    }

    public void SaveChanges()
    {
        Preset.SaveChanges();
    }
}
// 🎈