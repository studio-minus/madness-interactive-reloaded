using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class TelekinesisDisk : ImprobabilityDisk
{
    public TelekinesisDisk() : base(
    "Phantom Madness",
    Assets.Load<Texture>("textures/ui/modifiers/PhantomMadness.png").Value,
    "Grants telekinesis powers.")
    {
        AbilityDescriptors = [
            new (){
                Name = "Telekinesis",
                Description = "Move and use weapons with your mind",
            }
        ];
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        if (character.TryGetNextAbilitySlot(scene, out var slot))
            scene.AttachComponent(ent, new TelekinesisAbilityComponent(slot));
    }
}
