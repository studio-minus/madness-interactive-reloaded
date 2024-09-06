using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class EngineerDisk : ImprobabilityDisk
{
    public EngineerDisk() : base(
        "Engineer",
        Assets.Load<Texture>("textures/ui/modifiers/Engineer.png").Value,
        "You will be downgraded to Engineer level.")
    {
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        character.Stats = new CharacterStats(character.Stats)
        {
            DodgeAbility = character.Stats.DodgeAbility * 0.5f,
            MeleeSkill = character.Stats.MeleeSkill * 0.5f,
            AgilitySkillLevel = AgilitySkillLevel.Adept,
            JumpDodgeDuration = 0.25f
        };
    }
}
 //🎈