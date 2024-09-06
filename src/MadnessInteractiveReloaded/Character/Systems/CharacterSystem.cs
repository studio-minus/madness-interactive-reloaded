using System;
using Walgelijk;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Handles <see cref="CharacterComponent"/>s.
/// </summary>
public class CharacterSystem : Walgelijk.System
{
    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        foreach (var character in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            character.CurrentAttackerCount = 0;

            character.DodgeMeter = float.Min(character.Stats.DodgeAbility, character.DodgeMeter);
            if (character.DodgeRegenCooldownTimer <= float.Epsilon && character.Stats.DodgeAbility > float.Epsilon)
            {
                if (character.Positioning.IsFlying || float.Abs(character.Positioning.HopAcceleration) > 0.2f )
                    character.DodgeMeter = Utilities.Clamp(character.DodgeMeter + Time.DeltaTime * 0.6f, 0, character.Stats.DodgeAbility);
            }
            else
                character.DodgeRegenCooldownTimer -= Time.DeltaTime;

            if (character.HasWeaponEquipped && Scene.TryGetComponentFrom<DespawnComponent>(character.EquippedWeapon.Entity, out var weaponDespawner))
                weaponDespawner.Timer = 0;
            //Draw.Colour = Colors.Magenta;
            //Draw.Line(character.DeflectionLine.A, character.DeflectionLine.B, 2);

            character.AdditionalConstraints = default;
            foreach (var item in Scene.GetAllComponentsFrom(character.Entity))
            {
                if (item is IAnimationConstraintComponent c)
                    character.AdditionalConstraints |= c.Constraints;
            }
        }
    }
}
