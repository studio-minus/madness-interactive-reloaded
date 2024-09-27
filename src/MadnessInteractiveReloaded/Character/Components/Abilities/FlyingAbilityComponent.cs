using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;

namespace MIR;

public class FlyingAbilityComponent : CharacterAbilityComponent
{
    public override string DisplayName => "Flying"; //👩
    private float progress = 0;
    private bool wasFlying = false;

    public FlyingAbilityComponent(AbilitySlot slot) : base(slot, AbilityBehaviour.Toggle)
    {
    }

    public override void StartAbility(AbilityParams a)
    {
    }

    public override void UpdateAbility(AbilityParams a)
    {
        const float transitionTime = 0.4f;

        if (IsUsing)
            progress += a.Time.DeltaTime / transitionTime;
        else
            progress -= a.Time.DeltaTime / transitionTime;

        progress = float.Clamp(progress, 0, 1);
        float anim = Easings.Cubic.Out(progress);

        float s = float.Sin(a.Time * 4) * float.Abs(a.Character.Positioning.FlyingVelocity * 0.002f);
        float targetHeight = Utilities.MapRange(-1, 1, 500, 600, s);
        // we should check for collisions with the ceiling to prevent clipping!
        if (Scene.TryGetSystem<PhysicsSystem>(out var phys))
        {
            var b = a.Character.Positioning.GlobalCenter;
            var hit = phys.Raycast(b, Vector2.UnitY, out var r, filter: CollisionLayers.BlockMovement | CollisionLayers.BlockPhysics);

            if (hit)
            {
                var hitHeight = r.Distance - CharacterConstants.HalfHeight * a.Character.Positioning.Scale;
                targetHeight = float.Min(targetHeight, hitHeight);
            }
        }
        a.Character.Positioning.FlyingOffset = float.Lerp(0, targetHeight, anim);

        if(wasFlying && !a.Character.Positioning.IsFlying)
        {
            Audio.PlayOnce(Utilities.PickRandom(Sounds.SoftBodyImpact), 0.5f, Utilities.RandomFloat(0.9f, 1.1f), AudioTracks.SoundEffects);
        }

        wasFlying = a.Character.Positioning.IsFlying;
    }

    public override void EndAbility(AbilityParams a)
    {
    }
}
