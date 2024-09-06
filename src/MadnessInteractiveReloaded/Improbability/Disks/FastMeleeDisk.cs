using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class FastMeleeDisk : ImprobabilityDisk
{
    public FastMeleeDisk() : base(
        "Armstrong",
        Assets.Load<Texture>("textures/ui/modifiers/Armstrong.png").Value,
        "Increases your melee attack speed.")
    {
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        character.Stats = new CharacterStats(character.Stats)
        {
            MeleeSkill = character.Stats.MeleeSkill + 10,
        };
    }
}
// 🎈