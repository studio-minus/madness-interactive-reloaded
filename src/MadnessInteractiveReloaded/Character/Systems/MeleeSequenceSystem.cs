using System;
using System.Diagnostics.Metrics;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Runs <see cref="MeleeSequenceComponent"/> logic.
/// </summary>
public class MeleeSequenceSystem : Walgelijk.System
{
    private readonly MeleeSequenceComponent[] buffer = new MeleeSequenceComponent[256];

    public override void Update()
    {
        //Draw.Reset();
        //Draw.Order = RenderOrders.Effects;

        foreach (var comp in Scene.GetAllComponentsOfType<MeleeSequenceComponent>(buffer))
        {
            var entity = comp.Entity;
            if (comp.IsComplete)
            {
                Scene.DetachComponent<MeleeSequenceComponent>(entity);
                continue;
            }

            var ch = Scene.GetComponentFrom<CharacterComponent>(entity);
            var keyIndex = comp.CurrentIndex % comp.Sequence.Keys.Length;
            var key = comp.Sequence.Keys[keyIndex];

            if (ch.AnimationConstrainsAny(AnimationConstraint.PreventMelee))
            {
                Scene.DetachComponent<MeleeSequenceComponent>(entity);
                continue;
            }

            if (comp.LastAnim != null && comp.AnimationTimer < comp.LastAnim.Animation.TotalDuration * 0.99f)
            {
                var melee = comp.LastAnim;
                var currentKeyframe = (int)float.Ceiling(comp.AnimationTimer / melee.Animation.TotalDuration * melee.MaxKeyCount);

                if (comp.HitframesSpent < key.HitFrames.Length && currentKeyframe >= key.HitFrames[comp.HitframesSpent])
                {
                    Audio.PlayOnce(Utilities.PickRandom(Sounds.Swish), 0.3f, Utilities.RandomFloat(0.9f, 1.2f));
                    ProcessHitFrame(comp, ch, keyIndex == comp.Sequence.Keys.Length - 1);
                }

                if (comp.AnimationTimer > melee.Animation.TotalDuration * 0.9f) // we have this extra timer because the animation might loop and we could miss the end
                {
                    if (!comp.CanContinue)
                    {
                        comp.IsComplete = true;
                        continue;
                    }
                }

                if (currentKeyframe >= key.TransitionFrame)
                {
                    if (comp.CurrentIndex + 1 == comp.Sequence.Keys.Length)
                    {
                        comp.IsComplete = true;
                        continue;
                    }

                    //TODO je mag nog continueen proberen tot iets na de transitionframe

                    if (comp.CanContinue)
                    {
                        comp.LastAnim = null;
                        comp.CurrentIndex++;
                    }
                }

                comp.AnimationTimer += Time.DeltaTime * melee.Speed;
            }
            else
            {
                comp.CanContinue = false;
                comp.HitframesSpent = 0;
                comp.LastAnim = ch.PlayAnimation(key.Animation.Select(!ch.Positioning.IsFlipped), comp.Speed);
                comp.AnimationTimer = 0;
                //TODO WTF??? HIER MOET TOCH NIET !phys.IsFlipped staan wtf Oh nee

            }
        }
    }

    private void ProcessHitFrame(MeleeSequenceComponent comp, CharacterComponent ch, bool finalAttack)
    {
        comp.HitframesSpent++;

        // TODO "center" should not be the head position, but instead the default, unarmed hand position
        // this fixes issues relating to the character scale and melee "missing" when it really shouldn't 

        var center = ch.AimOrigin;
        var dir = ch.AimDirection;

        float damageMultiplier = float.Pow(ch.Positioning.Scale, 8);

        if (ch.EquippedWeapon.TryGet(Scene, out var wpn))
        {
            switch (wpn.Data.WeaponType)
            {
                case WeaponType.Firearm:
                    MeleeUtils.DoMeleeHit(Scene, ch, center, dir,
                        wpn.Data.Range * ch.Positioning.Scale,
                        (wpn.Data.ThrowableHeavy ? 0.25f : 0.125f) * ch.Stats.MeleeSkill * damageMultiplier, ch.EnemyCollisionLayer, wpn);
                    break;
                case WeaponType.Melee:
                    {
                        switch (wpn.Data.MeleeDamageType)
                        {
                            case MeleeDamageType.Axe:
                            case MeleeDamageType.Blade:
                                MeleeUtils.DoMeleeHit(Scene, ch, center, dir, wpn.Data.Range * ch.Positioning.Scale, wpn.Data.Damage * damageMultiplier, ch.EnemyCollisionLayer, wpn);
                                break;
                            case MeleeDamageType.Firearm: //wtf
                            case MeleeDamageType.Blunt:
                                MeleeUtils.DoMeleeHit(Scene, ch, center, dir,
                                    wpn.Data.Range * ch.Positioning.Scale,
                                    wpn.Data.Damage * damageMultiplier, ch.EnemyCollisionLayer, wpn);
                                break;
                        }
                    }
                    break;
            }
        }
        else
        {
            MeleeUtils.DoMeleeHit(Scene, ch, center, dir, 256 * ch.Positioning.Scale, 0.12f * ch.Stats.MeleeSkill * damageMultiplier, ch.EnemyCollisionLayer, wpn, finalAttack);
        }
    }
}