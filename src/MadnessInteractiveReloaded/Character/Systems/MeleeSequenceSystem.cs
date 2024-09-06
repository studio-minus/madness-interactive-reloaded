using System;
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

            if (comp.LastAnim != null && !comp.LastAnim.IsAlmostOver(0.99f))
            {
                var melee = comp.LastAnim;
                var currentKeyframe = (int)Math.Ceiling(melee.UnscaledTimer / melee.Animation.TotalDuration * melee.MaxKeyCount);

                if (comp.HitframesSpent < key.HitFrames.Length && currentKeyframe >= key.HitFrames[comp.HitframesSpent])
                {
                    Audio.PlayOnce(Utilities.PickRandom(Sounds.Swish), 0.3f, Utilities.RandomFloat(0.9f, 1.2f));
                    ProcessHitFrame(comp, ch, keyIndex == comp.Sequence.Keys.Length - 1);
                }

                if (melee.IsAlmostOver(0.9f))
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
            }
            else
            {
                comp.CanContinue = false;
                comp.HitframesSpent = 0;
                comp.LastAnim = ch.PlayAnimation(key.Animation.Select(!ch.Positioning.IsFlipped), comp.Speed);
                //TODO WTF??? HIER MOET TOCH NIET !phys.IsFlipped staan wtf Oh nee
          
            }
        }
    }

    private void ProcessHitFrame(MeleeSequenceComponent comp, CharacterComponent ch, bool finalAttack)
    {
        comp.HitframesSpent++;
        var center = ch.Positioning.Head.GlobalPosition;
        var dir = ch.AimDirection;
 
        if (ch.EquippedWeapon.TryGet(Scene, out var wpn))
        {
            switch (wpn.Data.WeaponType)
            {
                case WeaponType.Firearm:
                    MeleeUtils.DoMeleeHit(Scene, ch, center, dir,
                        wpn.Data.Range,
                        (wpn.Data.ThrowableHeavy ? 0.25f : 0.125f) * ch.Stats.MeleeSkill, ch.EnemyCollisionLayer, wpn);
                    break;
                case WeaponType.Melee:
                    {
                        switch (wpn.Data.MeleeDamageType)
                        {
                            case MeleeDamageType.Axe:
                            case MeleeDamageType.Blade:
                                MeleeUtils.DoMeleeHit(Scene, ch, center, dir, wpn.Data.Range, wpn.Data.Damage, ch.EnemyCollisionLayer, wpn);
                                break;
                            case MeleeDamageType.Firearm: //wtf
                            case MeleeDamageType.Blunt:
                                MeleeUtils.DoMeleeHit(Scene, ch, center, dir,
                                    wpn.Data.Range,
                                    wpn.Data.Damage, ch.EnemyCollisionLayer, wpn);
                                break;
                        }
                    }
                    break;
            }
        }
        else
        {
            MeleeUtils.DoMeleeHit(Scene, ch, center, dir, 256, 0.12f * ch.Stats.MeleeSkill, ch.EnemyCollisionLayer, wpn, finalAttack);
        }
    }
}