using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class AgentDisk : ImprobabilityDisk
{
    public AgentDisk() : base(
        "Agent",
        Assets.Load<Texture>("textures/ui/modifiers/Agent.png").Value,
        "You will be downgraded to Agent level.")
    {
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        character.Stats = new CharacterStats(character.Stats)
        {
            DodgeAbility = character.Stats.DodgeAbility * 0.25f,
            MeleeSkill = character.Stats.MeleeSkill * 0.25f,
            AgilitySkillLevel = AgilitySkillLevel.Novice,
            JumpDodgeDuration = 0.25f
        };
    }
} //🎈
