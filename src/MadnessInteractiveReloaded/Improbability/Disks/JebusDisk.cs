using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;
// 🙏 <-- this is here because when you make a new file
// VS goes insane and creates a file with null encoding
// so I add the prayer emoji to let it know to encode UTF8

public class JebusDisk : ImprobabilityDisk
{
    public JebusDisk() : base(
    "Savior",
    Assets.Load<Texture>("textures/ui/modifiers/Savior.png").Value,
    "Grants Christoff-like powers.")
    { 
        AbilityDescriptors = [
            new (){
                Name = "Flying",
                Description = "Hover above the floor to increase movement speed and avoid melee attacks",
            },
            new (){
                Name = "Telekinesis",
                Description = "Move and use weapons with your mind",
            },
            new (){
                Name = "Holy shield",
                Description = "Manifest a forcefield in front of you to block incoming fire",
            }
        ];
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        if (character.TryGetNextAbilitySlot(scene, out var slot))
            scene.AttachComponent(ent, new FlyingAbilityComponent(slot));

        if (character.TryGetNextAbilitySlot(scene, out slot))
            scene.AttachComponent(ent, new TelekinesisAbilityComponent(slot));

        if (character.TryGetNextAbilitySlot(scene, out slot))
            scene.AttachComponent(ent, new HolyShieldAbilityComponent(slot));
    }
}
