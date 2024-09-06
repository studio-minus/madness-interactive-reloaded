using System.Collections.Generic;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Manages the behaviour of the final boss in the main story campaign
/// </summary>
public class RichBossSystem : Walgelijk.System
{
    public override void Initialise()
    {
        //var character = Prefabs.CreateCharacter(Scene, new CharacterPrefabParams
        //{
        //    Name = "MAG RICH",
        //    Bottom = new Vector2(500, 200),
        //    Faction = Registries.Factions["aahw"],
        //    Look = Registries.Looks["mag_rich"],
        //    Stats = Registries.Stats["mag_rich"],
        //    BodyFleshTexture = Assets.Load<Texture>("textures/bodies/gore/rich_body_flesh.png"),
        //    HeadFleshTexture = Assets.Load<Texture>("textures/bodies/gore/rich_head_flesh.png"),
        //});
    }

    public static void RichHandPose(HandPoseParams p)
    {
        var rich = p.Scene.GetComponentFrom<RichCharacterComponent>(p.Character.Entity);
        var charPos = p.Character.Positioning;

        var holdingHand = p.Character.Positioning.IsFlipped ? charPos.Hands[0] : charPos.Hands[1];
        var otherHand = p.Character.Positioning.IsFlipped ? charPos.Hands[1] : charPos.Hands[0];

        if (rich.VisiblePhase is RichCharacterComponent.Phase.Gun)
        {
            holdingHand.PosePosition = p.Character.AimDirection * 500 + new Vector2(
                rich.GunRecoil * charPos.FlipScaling * -50,
                100 + rich.GunRecoil * 100);

            holdingHand.PoseRotation = Utilities.VectorToAngle(p.Character.AimDirection) + rich.GunRecoil * 15 * charPos.FlipScaling;
            holdingHand.Look = (p.Equipped?.HoldStockHandPose ?? false) ? HandLook.HoldStock : HandLook.HoldPistol;
            holdingHand.ShouldFollowRecoil = true;

            {
                otherHand.ShouldFollowRecoil = charPos.IronSightProgress >= 0.9f;
                otherHand.PosePosition = new Vector2(charPos.FlipScaling * -100, -300);
                otherHand.PoseRotation = charPos.IsFlipped ? 180 : 0;

                otherHand.Look = HandLook.Fist;
            }
        }
        else
        {
            HandPosingFunctions.FistFight(p);
            otherHand.PoseRotation = 80;
        }
    }

    public override void Render()
    {
        bool shouldRenderUi = !MadnessUtils.IsPaused(Scene)
            && MadnessUtils.FindPlayer(Scene, out var player, out var playerCharacter)
            && playerCharacter.IsAlive
            && !MadnessUtils.EditingInExperimentMode(Scene);

        foreach (var rich in Scene.GetAllComponentsOfType<RichCharacterComponent>())
        {
            var character = Scene.GetComponentFrom<CharacterComponent>(rich.Entity);

            var body = Scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Body.Entity);
            var head = Scene.GetComponentFrom<BodyPartComponent>(character.Positioning.Head.Entity);

            // draw UI
            if (shouldRenderUi)
            {
                float health = float.Max(0, float.Min(body.Health / body.MaxHealth, head.Health / head.MaxHealth));

                if (float.Abs(health - rich.LastHealthRatio) > float.Epsilon)
                    rich.HealthBarFlashTimer = 0;

                var healthFlash = float.Clamp(1 - rich.HealthBarFlashTimer * 4f, 0, 1);
                var c = (Color)Utilities.Lerp(Colors.Red, Colors.White, healthFlash);

                rich.HealthBarFlashTimer += Time.DeltaTime;
                rich.LastHealthRatio = health;

                Draw.Reset();
                Draw.Order = RenderOrders.UserInterface;
                Draw.ScreenSpace = true;
                Draw.FontSize = 30;
                Draw.Font = Fonts.Toxigenesis;

                var p = new Vector2(Window.Width / 2, 10);
                Draw.Colour = c.WithAlpha(0.5f);
                Draw.Text(character.Name, p + Utilities.RandomPointInCircle(1, 5) * (healthFlash * 2 + 1), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Top);
                Draw.Colour = c.WithAlpha(0.9f);
                Draw.Text(character.Name, p + Utilities.RandomPointInCircle(0, 1) * (healthFlash * 2 + 1), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Top);

                var healthRect = new Rect(new Vector2(Window.Width / 2, 50), new Vector2(Window.Width / 2, 14));

                Draw.Colour = Colors.Black;
                Draw.Quad(healthRect);

                Draw.Colour = c;
                var s = healthRect.Expand(-2);
                s.MaxX = float.Lerp(s.MinX, s.MaxX, health);
                Draw.Quad(s);
            }

            // draw weapons
            {
                Draw.Reset();

                if (rich.VisiblePhase is RichCharacterComponent.Phase.Sword)
                {
                    var swordHand = character.Positioning.IsFlipped ? character.Positioning.Hands.First : character.Positioning.Hands.Second;
                    var swordTex = rich.Sword.Texture.Value;
                    rich.Sword.Rectangle = new Rect(swordHand.GlobalPosition + new Vector2(0, swordTex.Height / 2 - 200), swordTex.Size * 1.2f);
                    Draw.Order = rich.Sword.Order = swordHand.ApparentRenderOrder.OffsetOrder(-1);
                    Draw.Texture = swordTex;
                    Draw.TransformMatrix = rich.Sword.Transform =
                        Matrix3x2.CreateScale(character.Positioning.FlipScaling, 1, swordHand.GlobalPosition) *
                        Matrix3x2.CreateRotation(
                        float.DegreesToRadians(swordHand.GlobalRotation + (character.Positioning.IsFlipped ? 180 : 0)),
                        swordHand.GlobalPosition);
                    Draw.Quad(rich.Sword.Rectangle, 0);
                }
                else
                {
                    var pp = character.Positioning.Body.ComputedVisualCenter + new Vector2(-250 * character.Positioning.FlipScaling, 150);
                    rich.Sword.Rectangle = new Rect(pp, rich.Sword.Texture.Value.Size * 1.2f);

                    Draw.Order = rich.Sword.Order = character.BaseRenderOrder.OffsetOrder(1);
                    Draw.Texture = rich.Sword.Texture.Value;
                    Draw.TransformMatrix = rich.Sword.Transform =
                        Matrix3x2.CreateScale(character.Positioning.FlipScaling, 1, pp) *
                        Matrix3x2.CreateRotation(
                            float.DegreesToRadians(character.Positioning.Body.GlobalRotation + (character.Positioning.IsFlipped ? 180 : 180)),
                            pp);
                    Draw.Quad(rich.Sword.Rectangle, 0);
                }

                {
                    var offset = new Vector2(150, 35);

                    bool h = character.Positioning.IsFlipped ^ rich.VisiblePhase is not RichCharacterComponent.Phase.Gun;
                    var gunHand = h ? character.Positioning.Hands.First : character.Positioning.Hands.Second;
                    rich.BarrelPosition = gunHand.GlobalPosition + Utilities.RotatePoint(new Vector2(offset.X + 290, (offset.Y + 120) * character.Positioning.FlipScaling), gunHand.GlobalRotation);

                    var gunTex = rich.Gun.Texture.Value;
                    rich.Gun.Rectangle = new Rect(gunHand.GlobalPosition + offset, gunTex.Size);

                    var gunSlideTex = rich.GunSlide.Texture.Value;
                    rich.GunSlide.Rectangle = new Rect(gunHand.GlobalPosition + offset + new Vector2(15, 125), gunSlideTex.Size);

                    float slideCurve = float.Clamp(rich.GunSlideTimer * 5, 0, 1);
                    if (rich.GunSlideTimer < 1)
                    {
                        slideCurve = float.Sin(slideCurve * float.Pi);
                        rich.GunSlide.Rectangle = rich.GunSlide.Rectangle.Translate(slideCurve * -90, 0);
                    }

                    Draw.Order = rich.Gun.Order = gunHand.ApparentRenderOrder.OffsetOrder(-1);
                    Draw.TransformMatrix = rich.Gun.Transform = rich.GunSlide.Transform =
                        Matrix3x2.CreateScale(character.Positioning.FlipScaling, 1, gunHand.GlobalPosition) *
                        Matrix3x2.CreateRotation(
                        float.DegreesToRadians(gunHand.GlobalRotation + (character.Positioning.IsFlipped ? 180 : 0)),
                        gunHand.GlobalPosition);

                    Draw.Texture = gunTex;
                    Draw.Quad(rich.Gun.Rectangle, 0);

                    Draw.Texture = gunSlideTex;
                    Draw.Quad(rich.GunSlide.Rectangle, 0);

                    Draw.Reset();
                }
            }
        }
    }

    public override void Update()
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var playerCharacter)
            || MadnessUtils.IsPaused(Scene))
            return;

        var isEditingInExp = MadnessUtils.EditingInExperimentMode(Scene);

        foreach (var rich in Scene.GetAllComponentsOfType<RichCharacterComponent>())
        {
            var character = Scene.GetComponentFrom<CharacterComponent>(rich.Entity);
            var entity = character.Entity;

            rich.GunSlideTimer += Time.DeltaTime;
            rich.GunRecoilVel += (0 - rich.GunRecoil) * 0.5f;
            rich.GunRecoilVel = Utilities.SmoothApproach(rich.GunRecoilVel, 0, 15, Time.DeltaTime);
            rich.GunRecoil += rich.GunRecoilVel * Time.DeltaTime;
            character.Positioning.CurrentRecoil = rich.GunRecoil * 1.2f;

            Draw.Order = character.BaseRenderOrder;

            character.Positioning.HopAnimationDuration = 0.4f;

            if (!character.IsAlive)
            {
                if (rich.CurrentPhase != RichCharacterComponent.Phase.Dying)
                {
                    if (Scene.TryGetSystem<LevelProgressSystem>(out var lps))
                        lps.Win();

                    character.StopAllAnimations();

                    CharacterAnimation? anim = rich.CurrentPhase switch
                    {
                        RichCharacterComponent.Phase.Sword => Registries.Animations.Get("rich_death_sword"),
                        RichCharacterComponent.Phase.Gun => Registries.Animations.Get("rich_death_gun"),
                        _ => Registries.Animations.Get("rich_death_unarmed"),
                    };

                    var pa = character.PlayAnimation(anim);
                    rich.CurrentPhase = RichCharacterComponent.Phase.Dying;

                    var snd = SoundCache.Instance.LoadSoundEffect(Assets.Load<FixedAudioData>("sounds/rich_boss_battle/death.wav"));
                    Audio.PlayOnce(snd);

                    // 3.2 is when the animation reaches the floor. yes its hardcoded lol
                    MadnessUtils.DelayPausable(3.2f, () => MadnessUtils.Shake(100));

                    // if the animation is over stamp it onto the background immediately
                    pa.OnEnd += () =>
                    {
                        if (!character.HasBeenRagdolled && Scene.HasEntity(character.Entity))
                        {
                            Stamper.Stamp(Scene, rich.Sword);
                            Stamper.Stamp(Scene, character.Positioning);
                            Stamper.Stamp(Scene, rich.Gun);
                            Stamper.Stamp(Scene, rich.GunSlide);
                            character.Delete(Scene);
                        }
                    };
                }
                return;
            }

            rich.ActiveRoutines.RemoveAll(RoutineScheduler.IsOngoing);

            if (!character.IsPlayingAnimation)
            {
                rich.TargetPosition = playerCharacter.Positioning.Head.GlobalPosition;
                rich.TargetPosition.Y += 70;
            }

            float dist = float.Abs(rich.TargetPosition.X - character.Positioning.GlobalCenter.X);

            if (!character.IsPlayingAnimation && dist > 300)
            {
                character.AimTargetPosition = Utilities.SmoothApproach(
                    character.AimTargetPosition,
                    rich.TargetPosition + MadnessUtils.Noise2D(Time * 0.7f, 95829f) * 150,
                    2, Time.DeltaTime);

                character.RelativeAimTargetPosition = character.AimTargetPosition - character.Positioning.GlobalCenter;

                var flipped = character.AimTargetPosition.X < character.Positioning.GlobalCenter.X;
                if (flipped != character.Positioning.IsFlipped)
                {
                    character.Positioning.IsFlipped = flipped;
                    character.NeedsLookUpdate = true;
                }
            }

            float maxDistanceToPlayer = rich.VisiblePhase is RichCharacterComponent.Phase.Gun ? 2500 : 1200;
            if (Utilities.Snap(dist, 300) > maxDistanceToPlayer) // we snap the distance so that Rich does not care about tiny changes
            {
                float x = float.Clamp(character.AimTargetPosition.X - character.Positioning.GlobalCenter.X, -500, 500);
                character.WalkAcceleration.X = Utilities.SmoothApproach(character.WalkAcceleration.X, x, 2, Time.DeltaTime);
            }
            else
                character.WalkAcceleration.X = 0;

            bool isFacingTarget = character.Positioning.IsFlipped == (rich.TargetPosition.X < character.Positioning.GlobalCenter.X);

            if (playerCharacter.IsAlive)
            {
                // Chance based stuff is on this fixed rate clock
                for (int i = 0; i < rich.AttackClock.CalculateCycleCount(Time.DeltaTime); i++)
                {
                    if (!character.IsPlayingAnimation && rich.PhaseTimer > 3 && !rich.ShootGunSequenceActive)
                    {
                        // Switch phase stuff
                        if ((Utilities.RandomFloat() > 0.95f) && rich.PhaseTimer > 9)
                        {
                            if (rich.CurrentPhase == RichCharacterComponent.Phase.Sword)
                            {
                                rich.CurrentPhase = RichCharacterComponent.Phase.Gun;
                                character.PlayAnimation(Registries.Animations["rich_sword_to_unarmed"]);
                            }
                            else
                            {
                                rich.CurrentPhase = RichCharacterComponent.Phase.Sword;
                                character.PlayAnimation(Registries.Animations["rich_unarmed_to_sword"]);
                            }
                            rich.PhaseTimer = 0;
                        }

                        // Attack stuff (this has to go after phase switch because we might start playing animations)
                        const float unarmedDistance = 900;
                        if (dist < unarmedDistance && Utilities.RandomFloat() > 0.2f && isFacingTarget)
                        {
                            character.PlayAnimation(Registries.Animations.Get("rich_unarmed_attack"));
                            rich.ActiveRoutines.Add(MadnessUtils.DelayPausable(1.2f, () =>
                            {
                                if (character.IsAlive)
                                    UnarmedMeleeHit(rich, character);
                            }));
                        }
                        else if (rich.CurrentPhase == RichCharacterComponent.Phase.Sword && dist < 1500 && isFacingTarget)
                        {
                            if (Utilities.RandomFloat() > 0.3f)
                            {
                                character.PlayAnimation(Registries.Animations.Get("rich_sword_attack"));
                                rich.ActiveRoutines.Add(MadnessUtils.DelayPausable(1.54f, () =>
                                {
                                    if (character.IsAlive)
                                        SwordImpact(rich, character);
                                }));
                            }
                        }
                        else if (rich.CurrentPhase == RichCharacterComponent.Phase.Gun)
                        {
                            character.Positioning.HandMousePosition = default;
                            if ((!rich.ShootGunSequenceActive && Utilities.RandomFloat() > 0.92f))
                            {
                                if (character.IsAlive)
                                    ShootGun(rich, character);
                            }
                        }
                    }
                }

                rich.PhaseTimer += Time.DeltaTime;
                if (rich.VisiblePhase != rich.CurrentPhase && rich.PhaseTimer > 1) // transition ended
                    rich.VisiblePhase = rich.CurrentPhase;
            }
        }
    }

    private void ShootGun(RichCharacterComponent rich, CharacterComponent character)
    {
        rich.ShootGunSequenceActive = true;

        rich.ActiveRoutines.Add(RoutineScheduler.Start(StartGunSequence(rich, character)));
    }

    private IEnumerator<IRoutineCommand> StartGunSequence(RichCharacterComponent rich, CharacterComponent character)
    {
        // start warning sequence

        var warnSound = SoundCache.Instance.Load(
            new CachedSoundProfile(
                Assets.Load<FixedAudioData>("sounds/rich_boss_battle/shot_warning.wav"),
                false,
                null,
                AudioTracks.SoundEffects
            )
        );
        Audio.PlayOnce(warnSound);

        MadnessUtils.Flash(Colors.Red.WithAlpha(0.5f), 0.5f);

        const float warningSeqDuration = 1;
        float t = 0;
        while (t < 1)
        {
            Draw.Reset();
            Draw.Order = RenderOrders.Effects;

            float radius = 500;
            float p = Easings.Cubic.In(t);

            Draw.Colour = Colors.Red.WithAlpha(0.1f);
            Draw.OutlineColour = Colors.Red;
            Draw.OutlineWidth = 24;
            Draw.Circle(rich.TargetPosition, new Vector2(radius) * p);

            Draw.OutlineColour = Colors.Red.WithAlpha(p);
            Draw.Colour.A = Draw.OutlineColour.A * 0.02f * p;
            Draw.Circle(rich.TargetPosition, new Vector2(Utilities.MapRange(0, 1, 4000, radius, p)));

            // crosshair lines
            {
                Draw.OutlineWidth = 0;
                Draw.Colour = Colors.White.WithAlpha(1 - float.Clamp(p * 12, 0, 1));
                Draw.Colour.A = float.Max(p * p, Draw.Colour.A);
                Draw.Colour.G = Draw.Colour.B = 1 - p;
                float r = 200 + p * p * 320;

                Draw.Line(rich.TargetPosition + new Vector2(-r, 0), rich.TargetPosition with { X = -9000 }, 16);
                Draw.Line(rich.TargetPosition + new Vector2(r, 0), rich.TargetPosition with { X = 9000 }, 16);
                Draw.Line(rich.TargetPosition + new Vector2(0, r), rich.TargetPosition with { Y = 9000 }, 16);
                Draw.Line(rich.TargetPosition + new Vector2(0, -r), rich.TargetPosition with { Y = -9000 }, 16);
            }

            t += Time.DeltaTime / warningSeqDuration;

            if (!character.IsAlive)
                yield break;

            yield return new GameSafeRoutineDelay(0);

            if (t >= 1)
                break;
        }

        // actually shoot
        var audio = Utilities.PickRandom(Assets.EnumerateFolder("sounds/rich_boss_battle/gun"));
        var snd = SoundCache.Instance.LoadSoundEffect(Assets.Load<FixedAudioData>(audio));
        Audio.PlayOnce(snd, 10, 1, AudioTracks.SoundEffects);
        Prefabs.CreateMuzzleFlash(Scene,
            rich.BarrelPosition + character.AimDirection * 300,
            float.Atan2(character.AimDirection.Y, character.AimDirection.X) * Utilities.RadToDeg,
            4);
        MadnessUtils.Shake(250);
        MadnessUtils.Flash(Colors.Black, 0.1f);
        MadnessUtils.Flash(Colors.Red, 0.4f);
        rich.GunRecoil += 0.5f;
        rich.GunRecoilVel += 25;
        rich.GunSlideTimer = 0;

        var bullet = rich.BarrelPosition;
        var bdir = character.AimDirection;
        for (int i = 0; i < 64; i++)
        {
            if (Scene.GetSystem<PhysicsSystem>().Raycast(bullet, bdir, out var hit, filter: CollisionLayers.AllCharacters | CollisionLayers.BlockBullets, ignore: character.AttackIgnoreCollision))
            {
                bullet = hit.Position;
                if (Scene.TryGetComponentFrom<BodyPartComponent>(hit.Entity, out var hitBodyPart))
                {
                    var victim = hitBodyPart.Character.Get(Scene);

                    if (victim.Entity == rich.Entity || Scene.HasComponent<RichBossAbilityComponent>(victim.Entity))
                        continue;

                    if (victim.Flags.HasFlag(CharacterFlags.Invincible)
                        || victim.AnimationConstrainsAny(AnimationConstraint.PreventDying | AnimationConstraint.PreventBeingShot))
                        continue;

                    if (Scene.TryGetTag(victim.Entity, out var vt) && vt == Tags.Player && ImprobabilityDisks.IsEnabled("god"))
                        continue;

                    //hitBodyPart.Damage(90);
                    victim.Kill();
                    MadnessUtils.TurnIntoRagdoll(Scene,
                        victim, bdir * 7 + new Vector2(0, Utilities.RandomFloat(1, 12)),
                        Utilities.RandomFloat(2, 5) * -character.Positioning.FlipScaling);

                    if (Scene.TryGetComponentFrom<BodyPartShapeComponent>(hit.Entity, out var shape))
                    {
                        var hitTransform = Scene.GetComponentFrom<TransformComponent>(hit.Entity);
                        var localPoint = Vector2.Transform(hit.Position, hitTransform.WorldToLocalMatrix);

                        if (shape.HorizontalFlip)
                            localPoint.X *= -1;

                        shape.TryAddHole(localPoint.X, localPoint.Y, 0.5f);
                        shape.TryAddInnerCutoutHole(localPoint.X, localPoint.Y, 1.2f);
                    }
                }
                else
                    break;
            }
            else
            {
                bullet += bdir * 10000; // bullet exited the map
                break;
            }
        }

        Scene.GetSystem<BulletTracerSystem>().ShowTracer(rich.BarrelPosition, bullet, 15);

        yield return new GameSafeRoutineDelay(1);
        rich.ShootGunSequenceActive = false;
    }

    private void SwordImpact(RichCharacterComponent rich, CharacterComponent character)
    {
        var hitRect = new Rect(rich.TargetPosition, new Vector2(700));
        var results = new QueryResult[16]; // TODO this does not need to be allocated every time :)

        for (int i = 0; i < Scene.GetSystem<PhysicsSystem>().QueryRectangle(hitRect, ref results, CollisionLayers.AllCharacters); i++)
        {
            ref var r = ref results[i];
            if (Scene.TryGetComponentFrom<BodyPartComponent>(r.Entity, out var bodyPart))
            {
                var victim = bodyPart.Character.Get(Scene);

                if (victim.Entity == rich.Entity || Scene.HasComponent<RichBossAbilityComponent>(victim.Entity))
                    continue;

                if (victim.Flags.HasFlag(CharacterFlags.Invincible)
                    || victim.AnimationConstrainsAny(AnimationConstraint.PreventDying))
                    continue;

                if (Scene.TryGetTag(victim.Entity, out var vt) && vt == Tags.Player && ImprobabilityDisks.IsEnabled("god"))
                    continue;

                victim.Kill();

                MadnessUtils.TurnIntoRagdoll(
                    Scene, victim,
                    addVelocity: new Vector2(Utilities.RandomFloat(-8, 8), Utilities.RandomFloat(5, 15)),
                    addTorque: Utilities.RandomFloat(-8, 8));
            }
        }

        {
            var x = character.Positioning.GlobalCenter.X + (1300) * character.Positioning.FlipScaling;
            CreateGroundImpactSprite(x);
        }

        var audio = Utilities.PickRandom(Assets.EnumerateFolder("sounds/rich_boss_battle/sword"));
        //Assets.AssignLifetime(audio, new SceneLifetimeOperator());
        var snd = SoundCache.Instance.LoadSoundEffect(Assets.Load<FixedAudioData>(audio));
        Audio.PlayOnce(snd, 2, 1, AudioTracks.SoundEffects);

        MadnessUtils.Shake(120);
    }

    private void CreateGroundImpactSprite(float x)
    {
        var asset = Utilities.PickRandom(Assets.EnumerateFolder("textures/rich_sword_impact"));
        var tex = Assets.Load<Texture>(asset);
        var e = Scene.CreateEntity();
        var t = Scene.AttachComponent(e, new TransformComponent
        {
            Position = new Vector2(x, MadnessUtils.GetFloorLevelAt(x)) + Utilities.RandomPointInCircle(0, 10),
            Scale = tex.Value.Size * MadnessConstants.BackgroundSizeRatio * Utilities.RandomFloat(0.95f, 1.053f),
        });
        var s = Scene.AttachComponent(e, new SpriteComponent(tex.Value)
        {
            RenderOrder = RenderOrders.BackgroundDecals,
            HorizontalFlip = Utilities.RandomFloat() > 0.5f
        });
    }

    private void UnarmedMeleeHit(RichCharacterComponent rich, CharacterComponent character)
    {
        var hitRect = new Rect(rich.TargetPosition, new Vector2(700));
        var results = new QueryResult[16]; // TODO this does not need to be allocated every time :)

        for (int i = 0; i < Scene.GetSystem<PhysicsSystem>().QueryRectangle(hitRect, ref results, CollisionLayers.AllCharacters); i++)
        {
            ref var r = ref results[i];
            if (Scene.TryGetComponentFrom<BodyPartComponent>(r.Entity, out var bodyPart))
            {
                var victim = bodyPart.Character.Get(Scene);

                if (victim.Entity == rich.Entity || Scene.HasComponent<RichBossAbilityComponent>(victim.Entity))
                    continue;

                if (victim.Flags.HasFlag(CharacterFlags.Invincible)
                    || victim.AnimationConstrainsAny(AnimationConstraint.PreventDying))
                    continue;

                if (Scene.TryGetTag(victim.Entity, out var vt) && vt == Tags.Player && ImprobabilityDisks.IsEnabled("god"))
                    continue;

                victim.Kill();

                MadnessUtils.TurnIntoRagdoll(
                    Scene, victim,
                    addVelocity: new Vector2(character.Positioning.FlipScaling * 25, Utilities.RandomFloat(10, 20)),
                    addTorque: Utilities.RandomFloat(-12, 12));
            }
        }

        var snd = SoundCache.Instance.LoadSoundEffect(Assets.Load<FixedAudioData>("sounds/rich_boss_battle/punch.wav"));
        //Assets.AssignLifetime("sounds/rich_boss_battle/punch.wav", new SceneLifetimeOperator());

        Audio.PlayOnce(snd, 2, 1, AudioTracks.SoundEffects);

        MadnessUtils.Shake(20);
    }
}
