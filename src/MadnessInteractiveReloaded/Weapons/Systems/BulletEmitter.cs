using System;
using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Physics;

namespace MIR;

public static class BulletEmitter
{
    private static Scene Scene => Game.Main.Scene;
    private static AudioRenderer Audio => Game.Main.AudioRenderer;
    private static readonly QueryResult[] buffer = new QueryResult[8];

    public struct BulletParameters
    {
        public Vector2 Origin;
        public Vector2 Direction;
        public bool IsCosmetic;
        /// <summary>
        /// Should be 1 usually. If this bullet is part of a bigger shot, then set this to the amount of bullets in said shot.
        /// </summary>
        public int ClusterSize;

        public float Damage;

        public uint EnemyCollisionLayer;
        public IEnumerable<Entity> IgnoreCollisionSet;
        public bool CanBeDeflected;
        public bool CanBeAutoDodged;

        public CharacterComponent? OriginCharacter;
        /// <summary>
        /// Note that the data in this object has no effect on the bullet. 
        /// The other fields determine everything.
        /// This is just here to pass along in case of a <see cref="HitEvent"/>
        /// </summary>
        public WeaponComponent? OriginWeapon;

        public BulletParameters()
        {
            IgnoreCollisionSet = [];
            ClusterSize = 1;
        }

        public BulletParameters(WeaponComponent? weapon)
        {
            Damage = weapon?.Data.Damage ?? 0;
            OriginWeapon = weapon;
            CanBeDeflected = (weapon?.Data.CanBulletsBeDeflected ?? false) && weapon.Data.BulletsPerShot == 1;
            ClusterSize = weapon?.Data.BulletsPerShot ?? 1;
        }
    }

    public static void CastBulletRay(BulletParameters parameters) => CastBulletRay(parameters, 0, 0);

    private static void CastBulletRay(BulletParameters pp, float totalDistance, int iteration)
    {
        if (iteration >= 8)
            return;

        var origin = pp.Origin;
        var bulletDirection = pp.Direction;

        var physics = Scene.GetSystem<PhysicsSystem>();
        var isExitWound = physics.QueryPoint(origin + bulletDirection * 5, buffer, pp.EnemyCollisionLayer, ignore: pp.IgnoreCollisionSet) > 0;

        if (physics.Raycast(origin, bulletDirection, out var hit, filter: pp.EnemyCollisionLayer | CollisionLayers.BlockBullets, ignore: pp.IgnoreCollisionSet) && Scene.HasEntity(hit.Entity))
        {
            totalDistance += hit.Distance;

            Scene.GetSystem<BulletTracerSystem>().ShowTracer(origin, hit.Position);
            if ((hit.Body.FilterBits & CollisionLayers.BlockBullets) == CollisionLayers.BlockBullets)
            {
                var inDecalZone = physics.QueryPoint(hit.Position, buffer, CollisionLayers.DecalZone) > 0;
                if (inDecalZone)
                    Prefabs.CreateBulletHole(Scene, hit.Position, Utilities.VectorToAngle(hit.Normal), 50);
            }

            if (Vector2.Distance(origin, hit.Position) < 5)
                isExitWound = false;

            if (pp.IsCosmetic && iteration > 1)
                return;

            var hitTransform = Scene.GetComponentFrom<TransformComponent>(hit.Entity);
            var localPoint = Vector2.Transform(hit.Position, hitTransform.WorldToLocalMatrix);

            var victimIsPlayer = Scene.HasTag(hit.Entity, Tags.Player);
            if (victimIsPlayer && ImprobabilityDisks.IsEnabled("god"))
                return;

            CharacterComponent? victimChar = null;
            var hasBodyPart = Scene.TryGetComponentFrom<BodyPartComponent>(hit.Entity, out var bodyPart);

            var isCharacter = hasBodyPart && Scene.TryGetComponentFrom(bodyPart!.Character.Entity, out victimChar);
            bool hasDodged = false;

            if (isCharacter && (victimChar?.IsAlive ?? false))
            {
                var isDodging = victimChar.AnimationConstrainsAny(AnimationConstraint.PreventBeingShot);

                if (isDodging ||
                    victimChar.Flags.HasFlag(CharacterFlags.Invincible))
                {
                    hasDodged = true;

                    if (!victimIsPlayer)
                    {
                        // drain dodge from enemies a bit anyway
                        victimChar.DrainDodge(0.005f);
                    }
                }

                {
                    bool canDodge = victimChar.HasDodge() && !isDodging;

                    if (canDodge && Scene.TryGetComponentFrom<AiComponent>(bodyPart!.Character.Entity, out var a))
                        a.PanicLevel += ConVars.Instance.PanicMultiplier * victimChar.Stats.PanicIntensity;

                    // deflection

                    bool willDeflect = pp.ClusterSize == 1 && bulletDirection.X > 0 == victimChar.Positioning.IsFlipped;

                    bool deflectingWeapon =
                        canDodge &&
                        victimChar.IsMeleeBlocking
                        && victimChar.HasWeaponEquipped
                        && victimChar.EquippedWeapon.TryGet(Scene, out var victimEquipped)
                        && victimEquipped.Data.CanDeflectBullets
                        && pp.CanBeDeflected;

                    float armourDeflectChance = bodyPart != null
                        && bodyPart.Entity == victimChar.Positioning.Head.Entity ? victimChar.Look.GetHeadDeflectionChance() : victimChar.Look.GetBodyDeflectionChance();

                    bool deflectingArmour = !victimChar.Look.Cosmetic && armourDeflectChance > Utilities.RandomFloat();

                    willDeflect &= deflectingArmour || deflectingWeapon;

                    if (willDeflect)
                    {
                        if (deflectingWeapon)
                            victimChar.DrainDodge(ConVars.Instance.DeflectDodgeCost * pp.Damage); //minder dodge damage met zwaardiaan

                        if ((victimChar.HasDodge() || victimChar.Stats.DodgeOversaturate))
                        {
                            var perfectDeflect = deflectingWeapon && !pp.CanBeAutoDodged && victimChar.Positioning.MeleeBlockProgress < 1; // TODO convar
                            if (pp.CanBeAutoDodged || perfectDeflect) // you are allowed to deflect an accurate shot event if you time it right
                            {
                                var hitPosOnLine = hit.Position;

                                if (deflectingWeapon)
                                {
                                    hitPosOnLine = MadnessUtils.ClosestPointOnLine(victimChar.DeflectionLine.A, victimChar.DeflectionLine.B, hit.Position);
                                    victimChar.Positioning.MeleeBlockImpactIntensity += Utilities.RandomFloat(-1, 1);
                                }

                                if (perfectDeflect)
                                {
                                    if (pp.OriginCharacter != null)
                                    {
                                        pp.OriginCharacter.DodgeMeter = -1;
                                        pp.OriginCharacter.DodgeRegenCooldownTimer = 10;
                                    }
                                    victimChar.DodgeMeter = victimChar.Stats.DodgeAbility;
                                    victimChar.Positioning.MeleeBlockProgress = 1;
                                    victimChar.Positioning.MeleeBlockImpactIntensity += 1;
                                    victimChar.Positioning.CurrentRecoil += 2;

                                    // TODO should be in a system and the sound is not very nice
                                    Audio.PlayOnce(
                                        SoundCache.Instance.LoadSoundEffect(
                                            Assets.Load<FixedAudioData>("sounds/deflection/perfect_deflect_1.wav")), 2);
                                    const float d = 0.5f;
                                    MadnessUtils.RoutineForSecondsPausable(d, static (dt) =>
                                    {
                                        Game.Main.State.Time.TimeScale = Utilities.SmoothApproach(Game.Main.State.Time.TimeScale, 1, 1, dt);
                                    });
                                    MadnessUtils.DelayPausable(0.05f, static () => { Game.Main.State.Time.TimeScale = 0.2f; });
                                    MadnessUtils.DelayPausable(d, static () => { Game.Main.State.Time.TimeScale = 1; });
                                }
                                else
                                    Audio.PlayOnce(Utilities.PickRandom(Sounds.BulletDeflection), 0.5f);

                                if (deflectingArmour && victimChar.IsAlive && victimChar.Flags.HasFlag(CharacterFlags.StunAnimationOnNonFatalAttack))
                                {
                                    if (bulletDirection.X < 0 == victimChar.Positioning.IsFlipped)
                                        victimChar.PlayAnimation(Registries.Animations.Get("stun_light_forwards"), 1.2f);
                                    else
                                        victimChar.PlayAnimation(Registries.Animations.Get("stun_light_backwards"), 1.2f);
                                }

                                var returnDir = perfectDeflect
                                    ? Vector2.Normalize((pp.OriginCharacter?.Positioning.Head.GlobalPosition ?? pp.Origin) - hitPosOnLine)
                                    : Vector2.Normalize(bulletDirection * new Vector2(-1, Utilities.RandomFloat(-12, 12)));

                                pp.OriginCharacter = victimChar;

                                MadnessUtils.DelayPausable(0.05f, () =>
                                {
                                    CastBulletRay(pp with
                                    {
                                        Origin = hitPosOnLine,
                                        Direction = returnDir,
                                        OriginCharacter = victimChar,
                                        EnemyCollisionLayer = victimChar.EnemyCollisionLayer,
                                        IgnoreCollisionSet = victimChar.AttackIgnoreCollision,
                                        CanBeAutoDodged = true
                                    },
                                    totalDistance, iteration + 1);
                                });
                                Prefabs.CreateDeflectionSpark(Scene, hitPosOnLine, Utilities.VectorToAngle(returnDir), 1);
                                return;
                            }
                        }
                    }
                    else if (canDodge)
                    {
                        var bulletIsComingFromFacingDirection = bulletDirection.X > 0 == victimChar.Positioning.IsFlipped;
                        var dodgeCost = float.Max(pp.Damage, 0.4f) / 1.5f * (victimChar.Stats.DodgeOversaturate ? 1 : pp.ClusterSize);

                        if (!bulletIsComingFromFacingDirection)
                            dodgeCost *= ConVars.Instance.DodgeFromBehindCostMultiplier;

                        var distanceSquared = Vector2.DistanceSquared(hit.Position, origin);

                        if (!victimIsPlayer)
                        {
                            float pointBlankDistance = ConVars.Instance.PointBlankDistance;
                            const float minDodgeCurveDistance = 1600;
                            const float maxDodgeCurveDistance = 2500;

                            if (distanceSquared < (pointBlankDistance * pointBlankDistance))
                            {
                                if (!victimChar.Stats.DodgeOversaturate)
                                    dodgeCost *= ConVars.Instance.DodgePointBlankCostMultiplier;
                            }
                            else
                                dodgeCost /= Utilities.MapRange(
                                    minDodgeCurveDistance * minDodgeCurveDistance, maxDodgeCurveDistance * maxDodgeCurveDistance,
                                    1, 8,
                                    Utilities.Clamp(distanceSquared, minDodgeCurveDistance * minDodgeCurveDistance, maxDodgeCurveDistance * maxDodgeCurveDistance));
                        }

                        victimChar.DrainDodge(dodgeCost);

                        bool shouldJumpDodge = victimChar.Stats.AgilitySkillLevel != AgilitySkillLevel.None && !victimIsPlayer && Utilities.RandomFloat() < ConVars.Instance.JumpDodgeChance;
                        canDodge = victimChar.HasDodge() || victimChar.Stats.DodgeOversaturate || shouldJumpDodge;

                        // je kan nog steeds ontwijken omdat het pas net < 0 is geworden :)
                        if (canDodge && pp.CanBeAutoDodged)
                        {
                            if (shouldJumpDodge)
                                CharacterUtilities.TryJumpDodge(Scene, victimChar);
                            else
                                CharacterUtilities.TryDodgeAnimation(victimChar);

                            if (victimIsPlayer)
                                Audio.PlayOnce(Utilities.PickRandom(Sounds.NearMiss), 1f);
                            hasDodged = true;
                        }
                    }
                }
            }

            if (hasDodged)
                CastBulletRay(pp with { Origin = hit.Position, }, totalDistance, iteration + 1);
            else if (victimChar == null || victimChar.Flags.HasFlag(CharacterFlags.AttackResponseBullet))
            {
                //actual on hit

                if (Scene.TryGetComponentFrom<ShapeComponent>(hit.Entity, out var bodyPartRenderer))
                    if (bodyPartRenderer.HorizontalFlip)
                        localPoint.X *= -1;

                if (Scene.TryGetComponentFrom<IsShotTriggerComponent>(hit.Entity, out var trigger))
                    trigger.Event.Dispatch(new HitEvent
                    {
                        Normal = hit.Normal,
                        Point = hit.Position,
                        Params = pp
                    });

                if (isCharacter && victimChar != null)
                {
                    if (victimChar.IsAlive && totalDistance < 300 && Utilities.RandomFloat() > 0.95f)
                    {
                        var splatter = SoundCache.Instance.LoadSoundEffect(Assets.Load<FixedAudioData>("sounds/splatter.wav"));
                        Audio.PlayOnce(splatter, 1, Utilities.RandomFloat(0.95f, 1.05f));
                    }

                    //damage broken armour
                    if (hit.Entity == victimChar.Positioning.Head.Entity && Utilities.RandomFloat() > 0.5f)
                    {
                        var originalLook = victimChar.Look;
                        victimChar.Look = new CharacterLook(victimChar.Look); //copy for for the copy

                        if (originalLook.HeadLayer1?.TryGetBrokenReplacement(out var key) ?? false)
                            victimChar.Look.HeadLayer1 = Registries.Armour.HeadAccessory.Get(key);

                        if (originalLook.HeadLayer2?.TryGetBrokenReplacement(out key) ?? false)
                            victimChar.Look.HeadLayer2 = Registries.Armour.HeadAccessory.Get(key);

                        if (originalLook.HeadLayer3?.TryGetBrokenReplacement(out key) ?? false)
                            victimChar.Look.HeadLayer3 = Registries.Armour.HeadAccessory.Get(key);

                        victimChar.NeedsLookUpdate = true;
                    }
                }

                if (Scene.TryGetComponentFrom<ImpactOffsetComponent>(hit.Entity, out var impactOffset))
                {
                    float rot = 3;
                    rot *= bulletDirection.X > 0 ? 1 : -1;

                    if (localPoint.Y > hitTransform.LocalRotationPivot.Y)
                        impactOffset.RotationOffset += -rot;
                    else
                        impactOffset.RotationOffset += rot;

                    impactOffset.TranslationOffset += bulletDirection * 6/* / Math.Min(data.BulletsPerShot, 2)*/;
                }

                var wasAlive = victimChar?.IsAlive ?? false;

                if (victimChar != null && hasBodyPart && bodyPart != null && bodyPart.Character.IsValid(Scene))
                {
                    if (!victimChar.IsAlive && Scene.TryGetComponentFrom<VerletNodeComponent>(hit.Entity, out var ragdollNode))
                        ragdollNode.Acceleration += bulletDirection * pp.Damage * 1;

                    if (Utilities.RandomFloat() > 0.7f)
                    {
                        var angle = Utilities.RandomFloat(-25, 25) + Utilities.VectorToAngle(bulletDirection);
                        var size = Utilities.RandomFloat(250, 400);
                        Prefabs.CreateBloodSplat(Scene, hit.Position, angle, victimChar.Look.BloodColour, size);
                    }

                    CharacterUtilities.DoGunDamage(Scene, WeaponComponent.GetDamageAtDistance(pp.Damage, totalDistance), bodyPart, victimChar, bulletDirection, localPoint);

                    // increment player kills stat
                    if (pp.OriginCharacter != null && Scene.FindAnyComponent<GameModeComponent>(out var mode) && mode.Mode == GameMode.Campaign)
                        if (wasAlive && !victimChar.IsAlive && Scene.HasTag(pp.OriginCharacter.Entity, Tags.Player) && Level.CurrentLevel != null)
                            CampaignProgress.GetCurrentStats()?.IncrementKills(Level.CurrentLevel);

                    if (Scene.TryGetComponentFrom<AiComponent>(bodyPart.Character.Entity, out var ai))
                        ai.PanicLevel += 0.25f;
                }

                if (Scene.TryGetComponentFrom<BodyPartShapeComponent>(hit.Entity, out var damagable) && victimChar != null)
                {
                    var charScaleSqrd = victimChar.Positioning.Scale * victimChar.Positioning.Scale;
                    var randomness = Utilities.RandomPointInCircle(-0.1f, 0.1f);
                    randomness.X *= 2;
                    var p = localPoint + randomness;
                    var s = GetBulletHoleSize(totalDistance, pp.Damage, pp.ClusterSize) / (iteration + 1) * (isExitWound ? 2 : 0.5f);
                    if (pp.IsCosmetic)
                        s *= 0.5f;

                    //s *= 0;

                    s /= charScaleSqrd;
                    s = damagable.TryAddHole(p.X, p.Y, s);

                    Prefabs.CreateBloodSpurt(Scene, hit.Position, MathF.Atan2(bulletDirection.Y, bulletDirection.X) * Utilities.RadToDeg, damagable.BloodColour, Utilities.Clamp(s * 4, 1f, 1.4f));

                    // We should somehow only apply this to the apparel that is actually attached to this thing?
                    foreach (var dec in victimChar.Positioning.HeadDecorations)
                        if (Scene.TryGetComponentFrom<ApparelSpriteComponent>(dec, out var apparel) && apparel.Visible)
                        {
                            var decTransform = Scene.GetComponentFrom<TransformComponent>(dec);
                            var localPointOnDec = Vector2.Transform(hit.Position, decTransform.WorldToLocalMatrix);
                            apparel.TryAddHole(localPointOnDec.X, localPointOnDec.Y, s);
                        }
                    foreach (var dec in victimChar.Positioning.BodyDecorations)
                        if (Scene.TryGetComponentFrom<ApparelSpriteComponent>(dec, out var apparel) && apparel.Visible)
                        {
                            var decTransform = Scene.GetComponentFrom<TransformComponent>(dec);
                            var localPointOnDec = Vector2.Transform(hit.Position, decTransform.WorldToLocalMatrix);
                            apparel.TryAddHole(localPointOnDec.X, localPointOnDec.Y, s);
                        }

                    if (s > 0.2f)
                    {
                        var d = isExitWound ? bulletDirection : hit.Normal;
                        var n = Vector2.TransformNormal(d, hitTransform.WorldToLocalMatrix) * 180;
                        var newP = p + n * s;
                        damagable.TryAddInnerCutoutHole(newP.X, newP.Y, s + 1 / charScaleSqrd);
                    }

                    // make sure that corpses never block bullets
                    bool cosmeticBullet = !wasAlive;

                    cosmeticBullet = WeaponComponent.GetDamageAtDistance(pp.Damage, totalDistance) / (iteration + 1) < 0.4f;

                    // TODO is this even working as intended??
                    CastBulletRay(pp with
                    {
                        Origin = hit.Position,
                        IsCosmetic = cosmeticBullet
                    }, totalDistance, iteration + 1);
                    //CastBulletRay(hit.Position, bulletDirection, weapon, data, wielder, totalDistance, iteration + 1, cosmeticBullet);
                }

                if (victimChar != null &&
                    victimChar.Flags.HasFlag(CharacterFlags.StunAnimationOnNonFatalAttack) &&
                    victimChar.IsAlive && !isExitWound)
                {
                    if (bulletDirection.X < 0 == victimChar.Positioning.IsFlipped)
                        victimChar.PlayAnimation(Registries.Animations.Get("stun_light_forwards"), 1.2f);
                    else
                        victimChar.PlayAnimation(Registries.Animations.Get("stun_light_backwards"), 1.2f);
                }
            }
        }
        else
            Scene.GetSystem<BulletTracerSystem>().ShowTracer(origin, origin + bulletDirection * 10000);
    }

    /// <summary>
    /// Get how big a bullethole will be from a weapon.
    /// </summary>
    public static float GetBulletHoleSize(float totalDistance, float damage, int bulletsPerShot) =>
        (0.1f + WeaponComponent.GetDamageAtDistance(damage, totalDistance) * 0.07f) * float.Lerp(bulletsPerShot, 1, 0.55f);

}
