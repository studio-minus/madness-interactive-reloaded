using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class TrickyDisk : ImprobabilityDisk
{
    public TrickyDisk() : base(
        "003",
        Assets.Load<Texture>("textures/ui/modifiers/003.png").Value,
        "Grants you Tricky-like powers.")
    {
        AbilityDescriptors = [
            new (){
                Name = "Teleport",
                Description = "Teleports a short distance forwards",
            },   
            new (){
                Name = "Immortality",
                Description = "Simply revive yourself",
            },
        ];
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        character.Look = new CharacterLook(character.Look)
        {
            Jitter = true
        };

        character.Stats = new CharacterStats(character.Stats)
        {
            AgilitySkillLevel = AgilitySkillLevel.Master,
            DodgeAbility = 1,
            MeleeSkill = 45,
            HeadHealth = 5,
            BodyHealth = 5,
            MeleeKnockback = 8,
            RecoilHandlingAbility = 4,
        };

        var body = scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Body.Entity);
        var head = scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Head.Entity);
        body.MaxHealth = body.Health = 5;
        head.MaxHealth = head.Health = 5;

        character.Positioning.TopWalkSpeed *= 1.2f;
        character.Positioning.HopAnimationDuration *= 0.7f;

        if (character.TryGetNextAbilitySlot(scene, out var slot))
            scene.AttachComponent(ent, new TeleportDashAbilityComponent(slot));
    }
}
