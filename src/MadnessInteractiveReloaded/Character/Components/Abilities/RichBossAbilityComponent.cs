using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR;

public class RichBossAbilityComponent : CharacterAbilityComponent
{
    public RichBossAbilityComponent() : base(AbilitySlot.None, AbilityBehaviour.Always)
    {
    }

    public override string DisplayName => "MAG RICH";

    public override void Initialise(AbilityParams a)
    {
        var character = a.Character;

        var rich = Scene.AttachComponent(character.Entity, new RichCharacterComponent());

        character.Positioning.HandPoseFunctionOverride.Add(RichBossSystem.RichHandPose);
        var body = Scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Body.Entity);
        var head = Scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Head.Entity);

        Assets.AssignLifetime(rich.Sword.Texture.Id, new SceneLifetimeOperator());
        Assets.AssignLifetime(rich.Gun.Texture.Id, new SceneLifetimeOperator());
        Assets.AssignLifetime(rich.GunSlide.Texture.Id, new SceneLifetimeOperator());

        // remove flags that interfere with the boss battle thing
        character.Flags &= ~(
            CharacterFlags.AttackResponseMelee |
            CharacterFlags.AttackResponseThrownProjectile |
            CharacterFlags.DeleteRagdoll |
            CharacterFlags.StunAnimationOnNonFatalAttack);

        character.Flags |= CharacterFlags.NoRagdoll;

        Scene.DetachComponent<AiComponent>(character.Entity);
    }

    public override void StartAbility(AbilityParams a) { }
    public override void EndAbility(AbilityParams a) { }
    public override void UpdateAbility(AbilityParams a) { }
}
