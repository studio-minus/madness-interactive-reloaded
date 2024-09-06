using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class SoldatDisk : ImprobabilityDisk
{
    public SoldatDisk() : base(
        "Soldat",
        Assets.Load<Texture>("textures/ui/modifiers/Soldat.png").Value,
        "You will be downgraded to Soldat level.")
    {
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        character.Stats = new CharacterStats(character.Stats)
        {
            DodgeAbility = character.Stats.DodgeAbility * 0.8f,
            MeleeSkill = character.Stats.MeleeSkill * 0.8f,
            AgilitySkillLevel = AgilitySkillLevel.Master
        };
    }
}
//🎈