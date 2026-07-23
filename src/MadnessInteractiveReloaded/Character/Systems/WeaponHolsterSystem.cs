using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Positions holstered weapons (<see cref="CharacterComponent.HolsteredWeapon"/>) on their wielder's back
/// and drops them when the wielder dies or is ragdolled.
/// </summary>
public class WeaponHolsterSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene))
            return;

        foreach (var character in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            if (!character.HolsteredWeapon.TryGet(Scene, out var weapon))
                continue;

            if (!character.IsAlive || character.HasBeenRagdolled)
            {
                character.DropHolsteredWeapon(Scene);
                continue;
            }

            if (!Scene.TryGetComponentFrom<TransformComponent>(weapon.Entity, out var transform))
                continue;

            var body = character.Positioning.Body;
            var bodyTransform = Scene.GetComponentFrom<TransformComponent>(body.Entity);

            float flip = character.Positioning.FlipScaling;
            var scale = character.Positioning.Scale;

            weapon.IsFlipped = character.Positioning.IsFlipped;
            transform.LocalPivot = default;
            transform.Position = body.ComputedVisualCenter + new Vector2(-30 * flip * scale, 40 * scale);
            transform.Rotation = bodyTransform.Rotation + 65 * flip;
            transform.Scale = new Vector2(1, weapon.IsFlipped ? -1 : 1);

            // render behind the body but in front of whatever is behind the character
            weapon.RenderOrder = character.BaseRenderOrder.WithOrder(CharacterConstants.RenderOrders.BodyBaseOrder - 200);
        }
    }
}
