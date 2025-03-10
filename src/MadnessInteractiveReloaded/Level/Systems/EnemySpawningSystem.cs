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

            if (waveComponent.WaveIndex == -1 ||
                (waveComponent.ActiveWave != null && waveComponent.GetBodyCountThisWave(Scene) == waveComponent.ActiveWave.TargetCount))
            {
                waveComponent.WaveIndex++;
                waveComponent.BodyCountOnWaveStart = 0;
                if (waveComponent.WaveIndex >= waveComponent.Sequence.Waves.Length)
                    waveComponent.IsFinished = true;
                else
                    waveComponent.ActiveWaveEnemyCount = waveComponent.ActiveWave!.TargetCount;

                if (Scene.FindAnyComponent<LevelProgressComponent>(out var lvl))
                    waveComponent.BodyCountOnWaveStart = lvl.BodyCount.Current;

                waveComponent.SpawnTimer = -1; // give them some time... christ
            }

            var wave = waveComponent.ActiveWave;
            if (wave == null)
                return;

            waveComponent.SpawnTimer += Time.DeltaTime;
            if (waveComponent.SpawnTimer > wave.SpawnInterval && wave.Instructions.Length > 0)
            {
                waveComponent.SpawnTimer = Utilities.RandomFloat(-1, 1);
                TrySpawn(new SpawnParams
                {
                    WaveComponent = waveComponent,
                    SpawnInstructions = Utilities.PickRandom(wave.Instructions),
                    Weapon = wave.Weapons.Length > 0 ? Registries.Weapons[Utilities.PickRandom(wave.Weapons)] : null,
                    WeaponChance = wave.WeaponChance,
                    SpawnProvider = waveComponent,
                    Player = playerCharacterComponent
                });
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

    private void TrySpawn(SpawnParams spawnParams)
    {
        if (!FindSpawnPoint(spawnParams.SpawnProvider.SpawnPoints, out var door, out var position))
            return;

        if (door != null)
        {
            spawnParams.Door = door;
            spawnParams.Point = door.Properties.SpawnPoint;

            if (!door.IsOpen && !door.IsBusyWithAnimation)
            {
                int amountToSpawn = GetNextSpawnCount(spawnParams);
                Logger.Log(amountToSpawn);
                if (amountToSpawn > 0)
                    routines.Add(RoutineScheduler.Start(DoorSpawnRoutine(spawnParams, amountToSpawn)));
            }
        }
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

        if (Level.CurrentLevel != null)
        {
            var lvl = Level.CurrentLevel;

            if (lvl.ProgressionType == ProgressionType.BodyCount && Scene.FindAnyComponent<LevelProgressComponent>(out var lvlProgress))
                remainingToSpawn -= (lvlProgress.BodyCount.Current - spawnParams.WaveComponent.BodyCountOnWaveStart);

            remainingToSpawn = int.Min(remainingToSpawn, lvl.MaxEnemyCount - livingEnemies - activeSpawnRoutines);
        }

        return int.Max(0, remainingToSpawn);
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
        if (spawnParams.Weapon == null)
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
            else if (MathF.Abs(direction.X) >= 0.01f)
            {
                character.Positioning.IsFlipped = direction.X < 0;
                float speed = Utilities.RandomFloat(150, 350);

                Scene.AttachComponent(character.Entity, new ExitDoorComponent(
                    charOnFloorPos,
                    charOnFloorPos + new Vector2(direction.X * speed, 0), 0.2f));
            }
        }
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
