using MIR.LevelEditor;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Physics;
using Walgelijk.SimpleDrawing;

namespace MIR;

/// <summary>
/// Train level specific system.
/// </summary>
public class MachinistLevelSystem : Walgelijk.System
{
    public override void Initialise()
    {
        if (Scene.TryGetEntityWithTag(Tags.TrainEngine, out var trainEngineEntity))
        {
            var isShot = Scene.AttachComponent(trainEngineEntity, new IsShotTriggerComponent());
            var engine = Scene.AttachComponent(trainEngineEntity, new TrainEngineComponent());

            isShot.Event.AddListener(OnHit);

            void explode()
            {
                //nu is het tijd om te ontploffen
                engine.HasExploded = true;
                Prefabs.CreateTrainExplosion(Scene, new Vector2(883.6808f, -100.45358f));

                {
                    var overlayEntity = Scene.CreateEntity();
                    var transform = Scene.AttachComponent(overlayEntity, new TransformComponent());
                    var quad = Scene.AttachComponent(overlayEntity, new QuadShapeComponent(false));
                    var tex = Assets.Load<Texture>("textures/backgrounds/background_train_machinist_b.qoi");
                    quad.Material = SpriteMaterialCreator.Instance.Load(tex.Value); //TODO unload at some point?
                    quad.RenderTask.Material = quad.Material;

                    Assets.AssignLifetime(tex.Id, new SceneLifetimeOperator());
                    Assets.LinkDisposal(tex.Id, quad.Material);

                    quad.RenderOrder = RenderOrders.BackgroundBehind.WithOrder(1);

                    transform.Position = new Vector2(-40, -632);
                    transform.Scale = tex.Value.Size * MadnessConstants.BackgroundSizeRatio;
                }

                Prefabs.CreateFire(Scene, new Vector2(1088, -43), 0);
                Prefabs.CreateFire(Scene, new Vector2(775, -380), 1);

                var allBulletHoles = Scene.GetAllComponentsOfType<TrainEngineBulletHoleComponent>().ToArray();
                foreach (var b in allBulletHoles)
                    Scene.RemoveEntity(b.Entity);

                if (!Scene.HasSystem<LevelEditorTestSystem>())
                    MadnessUtils.DelayPausable(2, () =>
                    {
                        // (duston): Use Game.Main.Scene instead because the System's Scene reference could be stale (and *will be* like when exiting to the main menu/changing levels before the engine explodes)
                        if (Game.Main.Scene.HasSystem<MachinistLevelSystem>() && Game.Main.Scene.HasSystem<LevelProgressSystem>()) // are we still where we want to be?
                        {
                            var levelProgress = Scene.GetSystem<LevelProgressSystem>();
                            levelProgress.Win();
                            levelProgress.TransitionToNextLevel();
                        }
                    });
            }

            void OnHit(HitEvent e)
            {
                if (engine.HasExploded)
                    return;


                if (e.Weapon != null && e.Weapon.Data.WeaponType is WeaponType.Firearm && e.Weapon.HasRoundsLeft)
                {
                    Prefabs.CreateTrainEngineBulletHole(Scene, e.Point, e.Normal);
                    engine.Health -= 1;

                    if (ImprobabilityDisks.IsEnabled("infinite_ammo") && engine.Health <= 0)
                        explode();
                }
                else
                    explode();
            }
        }
    }

    public override void Render()
    {
        Draw.Reset();
        Draw.Colour = Colors.White;
        Draw.Order = RenderOrders.BackgroundBehind.WithOrder(2);

        foreach (var b in Scene.GetAllComponentsOfType<TrainEngineBulletHoleComponent>())
        {
            Draw.Texture = Utilities.PickRandom(Textures.EngineSparks).Value;
            var size = new Vector2(140, 100) * Utilities.RandomFloat(0.8f, 1.2f);
            var r = new Rect(b.Position + b.Normal * size.X / 6, size);
            Draw.Quad(r.TopLeft, r.GetSize());

            Draw.Line(b.Position + Utilities.RandomPointInCircle(0, 7), b.Position + b.Normal * Utilities.RandomFloat(10, 140) + Utilities.RandomPointInCircle(0, 25), Utilities.RandomFloat(7, 23));
        }

        if (Scene.FindAnyComponent<TrainEngineComponent>(out var engine) && !engine.HasExploded)
        {
            Draw.Texture = Textures.SteamEngineOverlay.Value;
            Draw.Colour = Colors.Red.WithAlpha(.5f * Utilities.Clamp(1 - ((Time.SecondsSinceLoad * 2) % 1)));
            Draw.Quad(new Vector2(375, 110) * MadnessConstants.BackgroundSizeRatio, Draw.Texture.Size * MadnessConstants.BackgroundSizeRatio);
        }
    }

    public override void Update()
    {
        if (!Scene.FindAnyComponent<PhysicsWorldComponent>(out var physics))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var playerComponent, out var playerCharacter))
            return;

        bool ShouldSpawnNewWeaponFuckThisWholeThing()
        {
            if (Scene.FindAnyComponent<TrainEngineComponent>(out var engine) && !engine.HasExploded)
            {
                bool alreadyOnGround = Scene.GetAllComponentsOfType<WeaponComponent>().Any(static s => s.Data.WeaponType == WeaponType.Firearm && s.HasRoundsLeft);

                if (alreadyOnGround)
                    return false;

                if (playerCharacter.HasWeaponEquipped && playerCharacter.EquippedWeapon.TryGet(Scene, out var eq))
                {
                    if (eq.Data.WeaponType is WeaponType.Firearm && !eq.HasRoundsLeft)
                        return true;

                    if (eq.Data.WeaponType is WeaponType.Melee)
                        return true;
                }
                else // we have no weapon
                    return true;
            }

            return false;
        }

        if (ShouldSpawnNewWeaponFuckThisWholeThing())
        {
            Prefabs.CreateWeapon(Scene, playerCharacter.Positioning.GlobalCenter, Registries.Weapons.Get("micro_uzi"));
        }

        foreach (var character in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            if (!Scene.TryGetTag(character.Entity, out var tag) || tag != Tags.EnemyAI)
                continue;

            var entity = character.Entity;
            var ai = Scene.GetComponentFrom<AiComponent>(entity);
            character.AllowWalking = false;
            ai.IsDocile = true;
            character.Positioning.IsFlipped = true;
            ai.AimingPosition = new Vector2(-500, 0);
            character.Positioning.Head.Direction = new Vector2(1, 0);
            character.Positioning.Head.GlobalRotation = 0;

            bool isBeingThreatened = false;
            if (playerCharacter.HasWeaponEquipped && playerCharacter.IsIronSighting)
            {
                var weapon = playerCharacter.EquippedWeapon.Get(Scene);
                var weaponTransform = Scene.GetComponentFrom<TransformComponent>(playerCharacter.EquippedWeapon.Entity);
                var barrel = Vector2.Transform(weapon.BarrelEndPoint, weaponTransform.LocalToWorldMatrix);
                var barrelDirection = Vector2.TransformNormal(Vector2.UnitX, weaponTransform.LocalToWorldMatrix);

                if (Scene.GetSystem<PhysicsSystem>().Raycast(barrel, barrelDirection, out var result, filter: playerCharacter.Faction.AttackHitLayerComposite, ignore: playerCharacter.AttackIgnoreCollision))
                {
                    if (Scene.HasTag(result.Entity, Tags.EnemyAI))
                    {
                        if (character.Animations.All(a => a.Animation != Animations.Scared2))
                            character.PlayAnimation(Animations.Scared2);
                        isBeingThreatened = true;
                    }
                }
            }

            if ((character.IsAlive && !character.IsPlayingAnimationGroup("deaths")) && (character.MainAnimation == null || character.MainAnimation.IsAlmostOver(0.85f)))
                character.PlayAnimation(isBeingThreatened ? Animations.Scared2 : Animations.Scared1);
        }
    }
}
