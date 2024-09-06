using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class WickDisk : ImprobabilityDisk
{
    public WickDisk() : base(
        "Wick",
        Assets.Load<Texture>("textures/ui/modifiers/Wick.png").Value,
        "Makes you less vulnerable.")
    {
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        character.Stats = new CharacterStats(character.Stats)
        {
            DodgeAbility = character.Stats.DodgeAbility * 4f
        };
    }
} //🎈