using System;
using System.Collections.Generic;
using System.Numerics;
using Walgelijk;

namespace MIR;

/// <summary>
/// Spawn enemies based on <see cref="EnemySpawningComponent"/> or <see cref="WaveSpawningComponent"/> data.
/// </summary>
public class EnemySpawningSystem : Walgelijk.System
{
    private readonly List<Routine> routines = [];
    private readonly DoorComponent[] doorBuffer = new DoorComponent[32];
    private int currentlySpawning = 0;

    public override void OnDeactivate()
    {
        foreach (var r in routines)
            RoutineScheduler.Stop(r);

        routines.Clear();
        currentlySpawning = 0;
    }

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) ||
            MadnessUtils.EditingInExperimentMode(Scene) ||
            MadnessUtils.IsCutscenePlaying(Scene))
            return;

        if (!AiCharacterSystem.AutoSpawn)
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var playerComponent, out var playerCharacterComponent) || !playerCharacterComponent.IsAlive)
            return;

        if (Scene.FindAnyComponent<WaveSpawningComponent>(out var waveComponent))
        {
            if (waveComponent.Sequence.Waves.Length == 0 || waveComponent.IsFinished || !waveComponent.Enabled)
                return;

            if (waveComponent.WaveIndex == -1 || (waveComponent.ActiveWave != null && waveComponent.ActiveWaveBodyCount == waveComponent.ActiveWave.TargetCount))
            {
                waveComponent.WaveIndex++;
                waveComponent.ActiveWaveBodyCount = 0;
                waveComponent.WaveInstrSeqIndex = 0;
                if (waveComponent.WaveIndex >= waveComponent.Sequence.Waves.Length)
                {
                    // we reached the end of the waves, but sometimes the level progress is set up such that
                    // the player has to kill more enemies than the wave are configured to spawn.
                    // in this situation, we should just keep spawning them

                    if (Level.CurrentLevel != null)
                        switch (Level.CurrentLevel.ProgressionType)
                        {
                            case ProgressionType.BodyCount:
                                if (Scene.FindAnyComponent<LevelProgressComponent>(out var lvlProgress))
                                {
                                    int stillRemaining = lvlProgress.BodyCount.Target - lvlProgress.BodyCount.Current;
                                    if (stillRemaining > 0)
                                    {
                                        waveComponent.ActiveWaveEnemyCount = stillRemaining;
                                        waveComponent.WaveIndex--;
                                    }
                                }
                                break;
                            default:
                                waveComponent.IsFinished = true;
                                break;
                        }
                }
                else
                    waveComponent.ActiveWaveEnemyCount = waveComponent.ActiveWave!.TargetCount;

                waveComponent.SpawnTimer = -1; // give them some time... christ
            }

            var wave = waveComponent.ActiveWave;
            if (wave == null)
                return;

            waveComponent.SpawnTimer += Time.DeltaTime;
            if (waveComponent.SpawnTimer > wave.SpawnInterval && wave.Instructions.Length > 0)
            {
                waveComponent.SpawnTimer = Utilities.RandomFloat(-1, 1);
                var spawnInstr = wave.Mode switch
                {
                    WaveMode.Sequential => wave.Instructions[waveComponent.WaveInstrSeqIndex % wave.Instructions.Length],
                    _ => Utilities.PickRandom(wave.Instructions),
                };

                bool success = TrySpawn(new SpawnParams
                {
                    WaveComponent = waveComponent,
                    SpawnInstructions = spawnInstr,
                    Weapon = wave.Weapons.Length > 0 ? Registries.Weapons[Utilities.PickRandom(wave.Weapons)] : null,
                    WeaponChance = wave.WeaponChance,
                    SpawnProvider = waveComponent,
                    Player = playerCharacterComponent
                });

                if (success)
                    waveComponent.WaveInstrSeqIndex++;
            }

            if (Game.DevelopmentMode)
                DebugDraw.Text(default,
                    $"Wave {waveComponent.WaveIndex}/{waveComponent.Sequence.Waves.Length}\n" +
                    $"{waveComponent.ActiveWaveEnemyCount} enemies remain", 4);
        }
        else if (Scene.FindAnyComponent<EnemySpawningComponent>(out var spawningComponent))
        {

        }

        routines.RemoveAll(static r => !RoutineScheduler.IsOngoing(r));
    }

    private bool TrySpawn(SpawnParams spawnParams)
    {
        if (!FindSpawnPoint(spawnParams.SpawnProvider.SpawnPoints, out var door, out var position))
            return false;

        spawnParams.Door = door;
        spawnParams.Point = position;

        if (door != null)
        {
            if (!door.IsOpen && !door.IsBusyWithAnimation)
            {
                int amountToSpawn = GetNextSpawnCount(spawnParams);
                if (amountToSpawn > 0)
                    routines.Add(RoutineScheduler.Start(DoorSpawnRoutine(spawnParams, amountToSpawn)));
            }
        }
        else
        {
            spawnParams.Point = position;
            int amountToSpawn = GetNextSpawnCount(spawnParams);
            for (int i = 0; i < amountToSpawn; i++)
                SpawnEnemy(spawnParams);
        }

        return true;
    }

    private int GetNextSpawnCount(in SpawnParams spawnParams)
    {
        int activeSpawnRoutines = currentlySpawning;
        int livingEnemies = 0;

        foreach (var c in Scene.GetAllComponentsOfType<CharacterComponent>())
            if (c.IsAlive && !Scene.HasTag(c.Entity, Tags.Player) && c.Faction.IsEnemiesWith(spawnParams.Player.Faction))
                livingEnemies++;

        int remainingToSpawn = spawnParams.WaveComponent.ActiveWaveEnemyCount;
        remainingToSpawn -= activeSpawnRoutines;
        remainingToSpawn -= livingEnemies;

        remainingToSpawn -= spawnParams.WaveComponent.ActiveWaveBodyCount;

        int maxLivingEnemies = 4;
        if (Level.CurrentLevel != null)
            maxLivingEnemies = Level.CurrentLevel.MaxEnemyCount;

        remainingToSpawn = int.Min(remainingToSpawn, maxLivingEnemies - livingEnemies - activeSpawnRoutines);

        return remainingToSpawn > 0 ? 1 : 0; // TODO normally we could spawn more than 1, but since the spawninstructions are set only once, we would spawn a bunch of clones. until that is resolved (easy fix actually lol), we'll just stick to 1

        //return int.Max(0, remainingToSpawn > 1 ? Utilities.RandomInt(1, remainingToSpawn + 1 /*because exclusive*/) : remainingToSpawn);
    }

    private IEnumerator<IRoutineCommand> DoorSpawnRoutine(SpawnParams spawnParams, int amount)
    {
        if (amount == 0 || spawnParams.Door == null)
            yield break;

        var wasPortal = spawnParams.Door.Properties.IsPortal;
        spawnParams.Door.Properties.IsPortal = false; // the player should not be able to travel through this door while its being used
        spawnParams.Door.Open(Scene);
        currentlySpawning += amount;
        yield return new GameSafeRoutineDelay(spawnParams.Door.Properties.AnimationDuration * 1.5f);

        for (int i = 0; i < amount; i++)
        {
            SpawnEnemy(spawnParams);

            if (amount > 1)
                yield return new GameSafeRoutineDelay(Utilities.RandomFloat(0.1f, .4f));
        }

        yield return new GameSafeRoutineDelay(spawnParams.Door.Properties.AnimationDuration * 0.5f);
        spawnParams.Door.Close(Scene);
        currentlySpawning -= amount;
        spawnParams.Door.Properties.IsPortal = wasPortal;
    }

    private void SpawnEnemy(in SpawnParams spawnParams)
    {
        if (Level.CurrentLevel == null)
            throw new Exception("Level.CurrentLevel was null when trying to spawn an NPC. Can't spawn without a level.");

        var spawnPoint = spawnParams.Point;

        DebugDraw.Cross(spawnPoint, 320, Colors.Magenta, 1, RenderOrders.Effects);
        DebugDraw.Circle(spawnPoint, 320, Colors.Magenta, 1, RenderOrders.Effects);

        // (duston) IMPORTANT: always set the initial position to spawnPoint first no matter what,
        // i don't care if its in the sky. only then, afterwards will we set the position to onGround.
        // this ensures that the matrices are being calculated at the right location first.

        // (zooi): unless they are spawned from a door :)

        var floorPos = new Vector2(spawnPoint.X, Level.CurrentLevel.GetFloorLevelAt(spawnPoint.X));

        CharacterComponent character;
        if (spawnParams.SpawnInstructions.Weapon.HasValue)
        {
            var wpn = spawnParams.SpawnInstructions.Weapon.Value;
            character = Prefabs.CreateEnemyWithWeapon(
                Scene,
                spawnPoint,
                Registries.Weapons[wpn.Key],
                spawnParams.SpawnInstructions.Stats,
                spawnParams.SpawnInstructions.Look,
                spawnParams.SpawnInstructions.Faction);

            if (character.EquippedWeapon.TryGet(Scene, out var eq))
            {
                eq.InfiniteAmmo = wpn.InfiniteAmmo;
                if (wpn.Ammo > 0)
                    eq.RemainingRounds = wpn.Ammo;
            }
        }
        else if (spawnParams.Weapon == null)
        {
            character = Prefabs.CreateEnemy(
                Scene,
                spawnPoint,
                spawnParams.SpawnInstructions.Stats,
                spawnParams.SpawnInstructions.Look,
                spawnParams.SpawnInstructions.Faction);
        }
        else
        {
            character = Prefabs.CreateEnemyWithWeapon(
                Scene,
                spawnPoint,
                spawnParams.Weapon,
                spawnParams.SpawnInstructions.Stats,
                spawnParams.SpawnInstructions.Look,
                spawnParams.SpawnInstructions.Faction);
        }

        var floorOffset = CharacterConstants.GetFloorOffset(character.Positioning.Scale);
        var charOnFloorPos = new Vector2(floorPos.X, floorPos.Y + floorOffset);
        var finalSpawnPoint = spawnParams.Door == null ? spawnPoint : new Vector2(spawnPoint.X, spawnPoint.Y + floorOffset);

        character.Positioning.GlobalCenter = charOnFloorPos;
        character.Positioning.GlobalTarget = charOnFloorPos with { Y = 0 };

        if (spawnPoint.Y - charOnFloorPos.Y > 1000) // the spawner is way up in the air so we should play an animation
            character.PlayAnimation(Animations.SpawnFromSky);
        else if (spawnParams.Door != null)
        {
            character.Tint = Colors.Black;
            var door = spawnParams.Door;
            var direction = door.Properties.FacingDirection;
            direction.X = Utilities.NanFallback(direction.X);
            if (direction.Y < 0) // this door is facing the camera
                Scene.AttachComponent(character.Entity, new ExitDoorComponent(finalSpawnPoint, charOnFloorPos, 0.3f) { IsVertical = true });
            else if (float.Abs(direction.X) >= 0.01f)
            {
                character.Positioning.IsFlipped = direction.X < 0;
                float speed = Utilities.RandomFloat(150, 350);

                Scene.AttachComponent(character.Entity, new ExitDoorComponent(
                    charOnFloorPos,
                    charOnFloorPos + new Vector2(direction.X * speed, 0), 0.2f));
            }
        }

        var waveComponent = spawnParams.WaveComponent;
        character.OnDeath.AddListener(c => waveComponent.ActiveWaveBodyCount++);
    }

    private bool FindSpawnPoint(IList<Vector2> points, out DoorComponent? door, out Vector2 position)
    {
        door = null;
        position = default;
        var doors = Scene.GetAllComponentsOfType(doorBuffer); // TODO this is kind of slow... 

        bool hasAdditionalSpawnPoints = points.Count > 0;
        bool hasDoors = false;
        foreach (var d in doors)
            if (d.Properties.EnemySpawnerDoor)
            {
                hasDoors = true;
                break;
            }

        // there are no spawnpoints and no doors!! 
        if (!hasAdditionalSpawnPoints && !hasDoors)
        {
            Logger.Error("Attempt to spawn enemy without existing spawners!");
            return false;
        }

        // only spawnpoints are available
        if (hasAdditionalSpawnPoints && !hasDoors)
        {
            position = MadnessUtils.PickRandom(points);
            return true;
        }

        // only doors are available
        if (!hasAdditionalSpawnPoints && hasDoors)
        {
            door = MadnessUtils.PickRandom(doors);
            position = door.Properties.SpawnPoint;
            return true;
        }

        // there are both spawnpoints AND doors
        // this code looks weird because of nullability
        if (points.Count > 0 && hasDoors)
        {
            var ratio = doors.Length / (float)(points.Count + doors.Length);
            if (Utilities.RandomFloat() < ratio) // make sure we weigh the selection appropriately
            {
                door = MadnessUtils.PickRandom(doors);
                position = door.Properties.SpawnPoint;
                return true;
            }
            else
            {
                position = MadnessUtils.PickRandom(points);
                return true;
            }
        }

        return default;
    }

    private record struct SpawnParams
    {
        public CharacterComponent Player;
        public WaveSpawningComponent WaveComponent;
        public DoorComponent? Door;
        public Vector2 Point;
        public float WeaponChance;
        public WeaponInstructions? Weapon;
        public ISpawnInstructions SpawnInstructions;
        public IEnemySpawnProvider SpawnProvider;
    }
}
