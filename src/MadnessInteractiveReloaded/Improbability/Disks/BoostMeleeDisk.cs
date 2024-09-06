using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class BoostMeleeDisk : ImprobabilityDisk
{
    public BoostMeleeDisk() : base(
        "Arnold",
        Assets.Load<Texture>("textures/ui/modifiers/Arnold.png").Value,
        "Boosts your melee attack strength.")
    {
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        character.Stats = new CharacterStats(character.Stats)
        {
            MeleeSkill = character.Stats.MeleeSkill + 10,
            MeleeKnockback = character.Stats.MeleeKnockback + 8,
        };
    }
}
