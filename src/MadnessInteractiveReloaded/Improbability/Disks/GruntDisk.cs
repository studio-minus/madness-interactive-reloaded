using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class GruntDisk : ImprobabilityDisk
{
    public GruntDisk() : base(
        "Grunt",
        Assets.Load<Texture>("textures/ui/modifiers/Grunt.png").Value,
        "You will be downgraded to Grunt level.")
    {
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        character.Stats = new CharacterStats(character.Stats)
        {
            DodgeAbility = character.Stats.DodgeAbility * 0.1f,
            MeleeSkill = character.Stats.MeleeSkill * 0.1f,
            AgilitySkillLevel = AgilitySkillLevel.None,
            JumpDodgeDuration = 0
        };
    }
} //🎈🎈
