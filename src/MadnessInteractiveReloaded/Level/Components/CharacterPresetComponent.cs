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
        CharacterThumbnailCache.Instance.Unload(Preset.Look);
        if (Registries.Experiment.CharacterPresets.TryGetKeyFor(Preset, out var id))
        {
            CharacterPresetDeserialiser.Save(
                 Preset.Name,
                 Preset.Look,
                 Preset.Stats,
                 UserData.Paths.ExperimentCharacterPresets + id + ".preset");
            Registries.LoadCharacterPresets();
        }
    }
}
// 🎈