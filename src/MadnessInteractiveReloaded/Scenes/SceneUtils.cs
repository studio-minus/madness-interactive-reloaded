using MIR.LevelEditor;
using MIR.LevelEditor.Objects;
using System;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.AssetManager;
using Walgelijk.Onion;
using Walgelijk.ParticleSystem;
using Walgelijk.Physics;
using static MIR.CameraMovementComponent;

namespace MIR;

/// <summary>
/// Helper methods for commonly performed <see cref="Scene"/> related logic.
/// </summary>
public static class SceneUtils
{
    /// <summary>
    /// Prepare the main menu.
    /// </summary>
    public static Scene PrepareMenuScene(Game game, SceneId id, bool createBackground = true)
    {
        var scene = new Scene(game, id);
        scene.ScenePersistence = ScenePersistence.Persist;
        var camera = scene.CreateEntity();
        scene.AttachComponent(camera, new TransformComponent
        {
            Position = new Vector2(0, 1080)
        });
        scene.AttachComponent(camera, new CameraComponent
        {
            OrthographicSize = 3,
            PixelsPerUnit = 1,
            Clear = true,
            ClearColour = Colors.Black
        });

        //  game.Window.IsCursorLocked = false;
        game.Window.CursorAppearance = DefaultCursor.Default;

        scene.AttachComponent(scene.CreateEntity(), new TimeUniformComponent(Materials.TrainMovingBackground) { Speed = 2.5f });

        scene.AddSystem(new OnionSystem());
        //scene.AddSystem(new GuiSystem());
        scene.AddSystem(new AssetBrowserDialogSystem());
        scene.AddSystem(new ConfirmationDialogSystem());
        scene.AddSystem(new TimeUniformSystem());

        scene.AddSystem(new CameraSystem());
        scene.AddSystem(new ShapeRendererSystem());

        scene.AddSystem(new AnimationSystem());
        scene.AddSystem(new Walgelijk.Background.BackgroundSystem());

        scene.AttachComponent(scene.CreateEntity(), new GameModeComponent(GameMode.Unknown));
        scene.AttachComponent(scene.CreateEntity(), new PhysicsWorldComponent { ChunkSize = 1000, UpdatesPerSecond = 30, ChunkCapacity = 256 });

        scene.AddSystem(new DisclaimerRenderSystem());
        scene.AddSystem(new PhysicsSystem());
        scene.AddSystem(new TransformConstraintSystem());
        scene.AddSystem(new VerletPhysicsSystem());
        scene.AddSystem(new ImpactOffsetSystem());
        scene.AddSystem(new PlayerCharacterSystem());
        scene.AddSystem(new AiCharacterSystem());
        scene.AddSystem(new CharacterPositionSystem());
        scene.AddSystem(new CharacterMovementSystem());
        scene.AddSystem(new WeaponSystem());
        scene.AddSystem(new TransformSystem());
        scene.AddSystem(new VelocitySystem());
        scene.AddSystem(new DestructibleBodyPartSystem());
        scene.AddSystem(new ParticleSystem());
        scene.AddSystem(new CharacterSystem());
        scene.AddSystem(new LifetimeSystem());
        scene.AddSystem(new FlipbookSystem());
        scene.AddSystem(new DespawnSystem());
        scene.AddSystem(new MeasuredVelocitySystem());
        scene.AddSystem(new BulletTracerSystem());
        scene.AddSystem(new RemoveEntityOnKillSystem());
        scene.AddSystem(new PrefabPoolSystem());

        scene.AddSystem(new MusicPlaylistSystem());
        //scene.AttachComponent(scene.CreateEntity(), new MusicPlaylistComponent(
        //[
        //    SoundCache.Instance.LoadMusicNonLoop(Resources.Assets<StreamAudioData>("music/Lothyde/unusual_tranquillity.ogg")),
        //    SoundCache.Instance.LoadMusicNonLoop(Resources.Assets<StreamAudioData>("music/splitmek.ogg")),
        //]));

        if (createBackground)
        {
            var background = Assets.Load<Texture>("textures/red-black-gradient.png").Value;
            background.WrapMode = WrapMode.Mirror;
            Walgelijk.Background.CreateBackground(scene, background);
        }

        scene.UpdateSystems();

        return scene;
    }

    /// <summary>
    /// Creates (or loads from cache) a menu scene with one UI system
    /// </summary>
    public static Scene GetMenuScene(Game game, Walgelijk.System system, in SceneId? id)
    {
        if (id.HasValue && game.SceneCache.TryGet(id.Value, out var scene))
        {
            return scene ?? throw new Exception("Null scene has somehow been registered");
        }

        scene = SceneUtils.PrepareMenuScene(game, id ?? Guid.NewGuid().ToString());

        if (!id.HasValue)
            scene.ScenePersistence = ScenePersistence.Dispose;

        game.State.Time.TimeScale = 1;

        scene.AddSystem(system);

        return scene;
    }

    /// <summary>
    /// Prepare an in-game scene. (Playing)
    /// </summary>
    public static Scene PrepareGameScene(Game game, GameMode mode, bool withPlayer, Level? level) => PrepareGameScene(game, mode, null, withPlayer, level);

    /// <summary>
    /// Prepare an in-game scene. (Playing)
    /// </summary>
    public static Scene PrepareGameScene(Game game, GameMode mode, SceneId? id, bool withPlayer, Level? level)
    {
        if (level != null)
            Level.CurrentLevel = level;
        game.AudioRenderer.Stop(Sounds.DeathMusic);

        var scene = new Scene(game, id ?? Guid.NewGuid().ToString());
        scene.ScenePersistence = id.HasValue ? ScenePersistence.Persist : ScenePersistence.Dispose;

        //game.Window.IsCursorLocked = true;
        //game.Window.CursorAppearance = DefaultCursor.Crosshair;

        AiComponent.LastAccurateShotTime = float.MinValue;

        scene.AttachComponent(scene.CreateEntity(), new CasingParticleDictComponent());
        scene.AttachComponent(scene.CreateEntity(), new PauseComponent());
        scene.AttachComponent(scene.CreateEntity(), new GameModeComponent(mode));
        scene.AttachComponent(scene.CreateEntity(), new BackgroundBufferStorageComponent(new RenderTexture(1024, 1024, flags: RenderTargetFlags.None)));
        scene.AttachComponent(scene.CreateEntity(), new PhysicsWorldComponent { ChunkSize = 800, UpdatesPerSecond = 30, ChunkCapacity = 64 });
        scene.AttachComponent(scene.CreateEntity(), new TimeUniformComponent(Materials.TrainMovingBackground) { Speed = 2.5f });

        var backgroundBulletHoles = scene.CreateEntity();
        scene.SetTag(backgroundBulletHoles, Tags.BackgroundDecals);
        scene.AttachComponent(backgroundBulletHoles, new DecalRendererComponent(DecalType.BulletHole) { RenderOrder = RenderOrders.BackgroundDecals });
        var foregroundBulletHoles = scene.CreateEntity();
        scene.SetTag(foregroundBulletHoles, Tags.ForegroundDecals);
        scene.AttachComponent(foregroundBulletHoles, new DecalRendererComponent(DecalType.BulletHole) { RenderOrder = RenderOrders.ForegroundDecals });

        var backgroundBlood = scene.CreateEntity();
        scene.SetTag(backgroundBlood, Tags.BackgroundDecals);
        scene.AttachComponent(backgroundBlood, new DecalRendererComponent(DecalType.Blood)
        {
            RenderOrder = RenderOrders.BackgroundDecals,
            UseDecalMask = true
        });
        var foregroundBlood = scene.CreateEntity();
        scene.SetTag(foregroundBlood, Tags.ForegroundDecals);
        scene.AttachComponent(foregroundBlood, new DecalRendererComponent(DecalType.Blood)
        {
            RenderOrder = RenderOrders.ForegroundDecals,
            UseDecalMask = true
        });

        var camera = scene.CreateEntity();

        scene.AttachComponent(camera, new CameraShakeComponent());
        scene.AttachComponent(camera, new TransformComponent());
        scene.AttachComponent(camera, new CameraComponent
        {
            OrthographicSize = 3,
            PixelsPerUnit = 1,
            Clear = true,
            ClearColour = Colors.Black
        });
        var cameraMovement = scene.AttachComponent(camera, new CameraMovementComponent());

        scene.AddSystem(new DisclaimerRenderSystem());
        scene.AddSystem(new OnionSystem());
        scene.AddSystem(new AssetBrowserDialogSystem());
        scene.AddSystem(new ConfirmationDialogSystem());
        //scene.AddSystem(new GuiSystem());

        scene.AddSystem(new PauseSystem());
        scene.AddSystem(new CutscenePlayerSystem());
        scene.AddSystem(new DecalSystem());
        scene.AddSystem(new BackgroundBufferSystem() { ExecutionOrder = 100 });

        scene.AddSystem(new TimeUniformSystem());
        scene.AddSystem(new PhysicsSystem());
        scene.AddSystem(new SoundEffectPitchSystem());

        scene.AddSystem(new CursorControllerSystem());

        scene.AddSystem(new TransformConstraintSystem());
        scene.AddSystem(new Walgelijk.Background.BackgroundSystem());
        scene.AddSystem(new VerletPhysicsSystem());
        scene.AddSystem(new ImpactOffsetSystem());
        scene.AddSystem(new JumpDodgeSystem());
        scene.AddSystem(new AiCharacterSystem());
        scene.AddSystem(new PlayerCharacterSystem());
        scene.AddSystem(new ExitDoorSystem());
        scene.AddSystem(new CharacterPickupSystem());
        scene.AddSystem(new CharacterAbilitySystem());
        scene.AddSystem(new CharacterMovementSystem());
        scene.AddSystem(new CharacterPositionSystem());
        scene.AddSystem(new TransformSystem());
        scene.AddSystem(new VelocitySystem());
        scene.AddSystem(new CameraSystem());
        scene.AddSystem(new WeaponSystem());
        scene.AddSystem(new PlayerDeathSequenceSystem());
        scene.AddSystem(new DestructibleBodyPartSystem());
        scene.AddSystem(new AccurateShotSystem());
        scene.AddSystem(new ParticleSystem());
        scene.AddSystem(new CharacterSystem());
        scene.AddSystem(new LevelBorderSystem());
        scene.AddSystem(new DoorSystem());
        scene.AddSystem(new RagdollVoidSoundSystem());
        scene.AddSystem(new RagdollLifetimeSystem());
        scene.AddSystem(new RagdollSleepingSystem());
        scene.AddSystem(new MeleeSequenceSystem());
        scene.AddSystem(new LevelProgressTriggerSystem());
        scene.AddSystem(new AnimationTriggerSystem());
        scene.AddSystem(new LevelScriptSystem());
        scene.AddSystem(new MusicPlaylistSystem());
        scene.AddSystem(new UnlockDiskSystem());
        scene.AddSystem(new RichBossSystem());

        scene.AddSystem(new CameraMovementSystem());
        scene.AddSystem(new CameraShakeSystem());
        scene.AddSystem(new LifetimeSystem());
        scene.AddSystem(new AnimationSystem());
        scene.AddSystem(new FlipbookSystem());
        scene.AddSystem(new PlayerUISystem());
        scene.AddSystem(new DespawnSystem());
        scene.AddSystem(new MeasuredVelocitySystem());
        scene.AddSystem(new BulletTracerSystem());
        scene.AddSystem(new RemoveEntityOnKillSystem());

        scene.AddSystem(new CutsceneSystem());
        scene.AddSystem(new ShapeRendererSystem());
        scene.AddSystem(new PrefabPoolSystem());
#if DEBUG
        scene.AddSystem(new GameDebugSystem());
#endif

        if (UserData.Instances.Settings.Video.StampRagdolls)
            scene.AddSystem(new StampSystem());

        createEjection(new("textures/bullet_casings/9mm.png"), Sounds.CasingBrassCollide, EjectionParticle.Small, 4);
        createEjection(new("textures/bullet_casings/50ae.png"), Sounds.CasingBrassCollide, EjectionParticle.Medium, 3);
        createEjection(new("textures/bullet_casings/5.56.png"), Sounds.CasingBrassCollide, EjectionParticle.RifleCasing, 3);
        createEjection(new("textures/bullet_casings/12gauge.png"), Sounds.CasingShellCollide, EjectionParticle.ShotgunShell, 3);

        // lief he
        ParticlesComponent createEjection(AssetId path, Sound[] collisionSounds, EjectionParticle type, int columns) =>
            Prefabs.CreateCasingEjectionParticleSystem(scene, ParticleMaterialCreator.Instance.Load(Assets.Load<Texture>(path)), columns, collisionSounds, type);

        //Prefabs.CreateBulletImpactParticleSystem(scene);

        // player creation
        if (withPlayer)
        {
            var playerSpawn = level?.Objects.OfType<PlayerSpawn>().FirstOrDefault();
            var position = playerSpawn?.Position ?? default;

            // find portal ID in case we got here by walking through a door
            if (!string.IsNullOrWhiteSpace(SharedLevelData.TargetPortalID) && level != null)
            {
                var foundDoor = level.Objects.OfType<Door>().FirstOrDefault(d => d.Properties.PortalID == SharedLevelData.TargetPortalID);
                if (foundDoor != null)
                    position = foundDoor.Properties.SpawnPoint;
            }
            SharedLevelData.TargetPortalID = null;

            var player = Prefabs.CreatePlayer(scene, position);
            var playerEntity = player.Entity;

            if (playerSpawn != null && !string.IsNullOrWhiteSpace(playerSpawn.SpawnWeapon) && Registries.Weapons.TryGet(playerSpawn.SpawnWeapon, out var spawnWpn))
            {
                var w = Prefabs.CreateWeapon(scene, position, spawnWpn);
                scene.GetComponentFrom<CharacterComponent>(playerEntity).EquipWeapon(scene, w);
            }
            else if (level != null)
            {
                MadnessUtils.EquipStoredWeapon(level, scene, scene.GetComponentFrom<CharacterComponent>(playerEntity));
            }

            cameraMovement.Targets.Add(new PlayerTarget());
        }

        scene.UpdateSystems();

        if (level != null)
        {
            //if (level.OpeningCutscene != null)
            //    scene.AttachComponent(scene.CreateEntity(), new CutsceneComponent(Resources.Load<Cutscene>(level.OpeningCutscene)));

            //TODO stamp canvas moet een LevelObject zijn :)
            scene.AttachComponent(scene.CreateEntity(), new StampCanvasComponent(
                (int)level.LevelBounds.Width,
                (int)level.LevelBounds.Height,
                level.LevelBounds.GetCenter()));

            foreach (var item in level.Objects)
            {
                try
                {
                    item.SpawnInGameScene(scene);
                }
                catch (Exception e)
                {
                    Logger.Warn($"Failed to spawn a described object ({item.GetType().Name}) in the level: {e}");
                }
            }

            var spawner = scene.AttachComponent(scene.CreateEntity(), new EnemySpawningComponent
            {
                Doors = [.. level.Objects.OfType<Door>().Where(d => d.Properties.EnemySpawnerDoor)],
                SpawnPoints = [.. level.Objects.OfType<EnemySpawner>().Select(static e => e.Position)],
                SpawnInstructions = [.. level.EnemySpawnInstructions.Cast<ISpawnInstructions>()],
                MaxEnemyCount = level.MaxEnemyCount,
                Interval = level.EnemySpawnInterval,
                WeaponsToSpawnWith = level.Weapons,
                WeaponChance = level.WeaponChance
            });

            if (mode == GameMode.Campaign)
            {
                scene.AddSystem(new LevelProgressSystem());
                var p = scene.AttachComponent(scene.CreateEntity(), new LevelProgressComponent());
                p.BodyCount.Target = level.ProgressionType is ProgressionType.BodyCount ? level.BodyCountToWin : int.MaxValue;

                if (level.EnemySpawnInstructions.Count > 0)
                {
                    if (ImprobabilityDisks.IsEnabled("fewer_enemies"))
                    {
                        if (level.ProgressionType == ProgressionType.BodyCount)
                        {
                            if (level.MaxEnemyCount > 1)
                                level.MaxEnemyCount--;
                        }
                        spawner.Interval *= 1.4f;
                        if (level.ProgressionType is ProgressionType.BodyCount && p.BodyCount.Target > 5)
                            p.BodyCount.Target /= 2;
                    }

                    if (ImprobabilityDisks.IsEnabled("more_enemies"))
                    {
                        if (level.ProgressionType == ProgressionType.BodyCount)
                        {
                            if (level.MaxEnemyCount > 1)
                                level.MaxEnemyCount += 4;
                        }
                        spawner.Interval *= 0.1f;
                        if (level.ProgressionType is ProgressionType.BodyCount && p.BodyCount.Target > 1)
                            p.BodyCount.Target *= 2;
                    }
                }
            }
        }

        return scene;
    }
}
