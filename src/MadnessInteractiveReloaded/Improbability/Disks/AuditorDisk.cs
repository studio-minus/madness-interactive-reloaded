using Walgelijk;
using Walgelijk.AssetManager;

namespace MIR.Disks;

public class AuditorDisk : ImprobabilityDisk
{
    public AuditorDisk() : base(
    "HP",
    Assets.Load<Texture>("textures/ui/modifiers/HP.png").Value,
    "You will be imbued with Higher Powers.")
    {// 🍕
        AbilityDescriptors = [
            new (){
                Name = "Conjuring",
                Description = "Opens a menu to conjure up weapons and allies",
            }
        ];
    }

    public override void Apply(Scene scene, CharacterComponent character)
    {
        var ent = character.Entity;

        AddAuditorFire(scene, character);

        if (character.TryGetNextAbilitySlot(scene, out var slot))
            scene.AttachComponent(ent, new SpawnMenuAbilityComponent(slot));
    }

    public static void AddAuditorFire(Scene scene, CharacterComponent character)
    {
        if (!scene.HasSystem<AuditorFireSystem>())
            scene.AddSystem(new AuditorFireSystem());
        {
            var headTexture = Assets.Load<Texture>("textures/auditorHead_4x6.png");
            var bodyTexture = Assets.Load<Texture>("textures/auditorBody_4x6.png");
            var headFlipbook = new FlipbookComponent(FlipbookMaterialCreator.LoadMaterialFor(
                headTexture.Value, 6, 4, 24, Colors.White, true, headTexture.Id.GetHashCode()));
            headFlipbook.Duration = 1;
            scene.AttachComponent(scene.CreateEntity(), headFlipbook);

            var bodyFlipbook = new FlipbookComponent(FlipbookMaterialCreator.LoadMaterialFor(
                headTexture.Value, 6, 4, 24, Colors.White, true, bodyTexture.Id.GetHashCode()));
            bodyFlipbook.Duration = 1;
            scene.AttachComponent(scene.CreateEntity(), bodyFlipbook);

            var fireComponent = new AuditorFireComponent
            {
                Character = character,
                HeadFlipbook = headFlipbook,
                BodyFlipbook = bodyFlipbook,
                HeadTexture = headTexture,
                BodyTexture = bodyTexture
            };
            scene.AttachComponent(scene.CreateEntity(), fireComponent);
        }
    }
}
