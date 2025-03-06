using MIR.LevelEditor.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Walgelijk;
using Walgelijk.Physics;

namespace MIR;

/// <summary>
/// Spawn enemies based on <see cref="EnemySpawningComponent"/> data.
/// </summary>
public class EnemySpawningSystem : Walgelijk.System
{
    private readonly Stack<Routine> routines = [];

    public override void OnDeactivate()
    {
        while (routines.TryPop(out var r))
            RoutineScheduler.Stop(r);
    }

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        ProcessSpawners();
        ProcessWaves();
    }

    private void ProcessWaves()
    {
        if (!Scene.FindAnyComponent<WaveSpawningComponent>(out var waveComponent) || !waveComponent.Enabled)
            return;

        if (waveComponent.WaveIndex == -1 || waveComponent.RemainingEnemiesThisWave == 0)
        {
            waveComponent.WaveIndex++;
            waveComponent.RemainingEnemiesThisWave = waveComponent.Sequence.Waves[waveComponent.WaveIndex].TargetCount;
        }

        var currentWave = waveComponent.Sequence.Waves[waveComponent.WaveIndex];

        var hasLevelProgress = Scene.FindAnyComponent<LevelProgressComponent>(out var lvlProgress);
        if (!CanSpawnAnotherEnemy(currentWave.MaxEnemyCount, lvlProgress, 1, out var liveEnemyCount))
            return;

        CharacterComponent? playerChar = null;
        var hasPlayer = Scene.FindAnyComponent<PlayerComponent>(out var player) && Scene.TryGetComponentFrom(player.Entity, out playerChar);

        if (Time.SecondsSinceSceneChange > 1)
            waveComponent.SpawnTimer += Time.DeltaTime;

        bool hasWon = hasLevelProgress && (lvlProgress?.GoalReached ?? false);

        if (!hasWon &&
            AiCharacterSystem.AutoSpawn &&
            waveComponent.SpawnTimer > currentWave.SpawnInterval &&
            (!hasPlayer || (playerChar != null && playerChar.IsAlive)))
        {
            var weapons = currentWave.Weapons ?? Registries.Weapons.GetAllKeys();
            var enemies = currentWave.Instructions ?? [];
            var spawnPoint = GetRandomSpawnPoint(waveComponent.SpawnPoints ?? [], waveComponent.Doors ?? [], out var isDoor);
            waveComponent.SpawnTimer = Utilities.RandomFloat(-1, 1);

            if (enemies.Length == 0)
                return;

            float weaponChance = currentWave.WeaponChance;

            if (isDoor)
            {
                var door = GetDoorNearest(spawnPoint);
                if (door != null && !door.IsOpen && !door.IsBusyWithAnimation)
                {
                    int amountToSpawn = GetNumberOfEnemiesToSpawn(currentWave.MaxEnemyCount, liveEnemyCount, lvlProgress);
                    routines.Push(RoutineScheduler.Start(DoorSpawnRoutine(amountToSpawn, weaponChance, door, enemies, weapons, spawnPoint)));
                }
            }
            else
            {
                var instr = GetEnemySpawnInstructions(enemies);
                if (instr != null)
                    Spawn(weaponChance, weapons, instr, spawnPoint);
            }
        }
    }

    private void ProcessSpawners()
    {
        if (!Scene.FindAnyComponent<EnemySpawningComponent>(out var spawningComponent) || !spawningComponent.Enabled)
            return;

        var hasLevelProgress = Scene.FindAnyComponent<LevelProgressComponent>(out var lvlProgress);

        if (!CanSpawnAnotherEnemy(spawningComponent.MaxEnemyCount, lvlProgress, 1, out var liveEnemyCount))
            return;

        CharacterComponent? playerChar = null;
        var hasPlayer = Scene.FindAnyComponent<PlayerComponent>(out var player) && Scene.TryGetComponentFrom(player.Entity, out playerChar);

        if (Time.SecondsSinceSceneChange > 1) // just to give the player some time to adjust
            spawningComponent.SpawnTimer += Time.DeltaTime;

        bool hasWon = hasLevelProgress && (lvlProgress?.GoalReached ?? false);

        if (!hasWon &&
            AiCharacterSystem.AutoSpawn &&
            spawningComponent.SpawnTimer > spawningComponent.Interval &&
            (!hasPlayer || (playerChar != null /*&& !playerChar.IsLowOnDodge()*/ && playerChar.IsAlive)))
        {
            var weapons = spawningComponent.WeaponsToSpawnWith ?? Registries.Weapons.GetAllKeys();
            var enemies = spawningComponent.SpawnInstructions ?? [];
            var spawnPoint = GetRandomSpawnPoint(spawningComponent.SpawnPoints ?? [], spawningComponent.Doors ?? [], out var isDoor);
            spawningComponent.SpawnTimer = Utilities.RandomFloat(-1, 1);

            if (enemies.Count == 0)
                return;

            //float weaponChance = Scene.GetAllComponentsOfType<WeaponComponent>()
            //  .Count(c => !c.Wielder.IsValid(Scene)) > spawningComponent.DroppedWeaponAmountThreshold ? spawningComponent.WeaponChance * 0.1f : spawningComponent.WeaponChance;
            float weaponChance = spawningComponent.WeaponChance;

            if (isDoor)
            {
                var door = GetDoorNearest(spawnPoint);
                if (door != null && !door.IsOpen && !door.IsBusyWithAnimation)
                {
                    int amountToSpawn = GetNumberOfEnemiesToSpawn(spawningComponent.MaxEnemyCount, liveEnemyCount, lvlProgress);
                    routines.Push(RoutineScheduler.Start(DoorSpawnRoutine(amountToSpawn, weaponChance, door, enemies, weapons, spawnPoint)));
                }
            }
            else
            {
                var instr = GetEnemySpawnInstructions(enemies);
                if (instr != null)
                    Spawn(weaponChance, weapons, instr, spawnPoint);
            }
        }
    }

    private static ISpawnInstructions? GetEnemySpawnInstructions(IList<ISpawnInstructions>? enemies) 
        => (enemies == null || enemies.Count == 0) ? null : Utilities.PickRandom(enemies);

    private IEnumerator<IRoutineCommand> DoorSpawnRoutine(
        int amount,
        float weaponChance,
        DoorComponent door,
        IList<ISpawnInstructions> enemies,
        IEnumerable<string> weapons,
        Vector2 spawnPoint)
    {
        if (amount == 0)
            yield break;

        var wasPortal = door.Properties.IsPortal;
        door.Properties.IsPortal = false;
        door.Open(Scene);
        yield return new GameSafeRoutineDelay(door.Properties.AnimationDuration * 1.5f);

        for (int i = 0; i < amount; i++)
        {
            var instr = GetEnemySpawnInstructions(enemies);
            if (instr != null)
                Spawn(weaponChance, weapons, instr, spawnPoint, door);

            if (amount > 1)
                yield return new GameSafeRoutineDelay(Utilities.RandomFloat(0.1f, .4f));
        }

        yield return new GameSafeRoutineDelay(door.Properties.AnimationDuration * 0.5f);
        door.Close(Scene);
        door.Properties.IsPortal = wasPortal;
    }

    Weet je wat? Fuck deze hele class. Dit zuigt, en het is een oneindige rotzooi. Maak alles opnieuw.

    private int GetLiveEnemyCount()
    {
        if (!MadnessUtils.FindPlayer(Scene, out _, out var player))
            return 0;

        // TODO improve speed
        return Scene.GetAllComponentsOfType<CharacterComponent>().Count(c =>
            c.IsAlive
            && !Scene.HasTag(c.Entity, Tags.Player)
            && c.Faction.IsEnemiesWith(player.Faction)
        );
    }

    private int GetRemainingEnemyCount(int maxEnemyCount, int liveEnemyCount, int targetBodyCount, int currentBodyCount)
    {
        if (targetBodyCount > 0 && Level.CurrentLevel?.ProgressionType == ProgressionType.BodyCount)
            return int.Min(maxEnemyCount, targetBodyCount - currentBodyCount - liveEnemyCount);
        else
            return maxEnemyCount;
    }

    private int GetOpenDoorCount()
    {
        return Scene.GetAllComponentsOfType<DoorComponent>().Count(static d => d.IsOpen || d.IsBusyWithAnimation);
    }

    private bool CanSpawnAnotherEnemy(int maxEnemyCount, LevelProgressComponent? lvlProgress, int requestedAmount, out int liveEnemyCount)
    {
        var enemyCount = GetLiveEnemyCount();
        var remainingEnemyCount = GetRemainingEnemyCount(maxEnemyCount, enemyCount, lvlProgress?.BodyCount.Target ?? 0, lvlProgress?.BodyCount.Current ?? 0);
        var openDoors = GetOpenDoorCount();
        enemyCount += openDoors;
        liveEnemyCount = enemyCount;
        return enemyCount - remainingEnemyCount - 1 < -requestedAmount; // Why -1? There was a reason for this but I forgot completely
    }

    private int GetNumberOfEnemiesToSpawn(int maxEnemyCount, int liveEnemyCount, LevelProgressComponent? lvlProgress)
    {
        var max = GetRemainingEnemyCount(maxEnemyCount, liveEnemyCount, lvlProgress?.BodyCount.Target ?? 0, lvlProgress?.BodyCount.Current ?? 0);
        return max == 0 ? 0 : Utilities.RandomInt(1, int.Min(max, 3));
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <exception cref="Exception"></exception>
    private void Spawn(float weaponChance, IEnumerable<string>? weapons, ISpawnInstructions toSpawn, Vector2 spawnPoint, DoorComponent? applicableDoor = null)
    {
        if (Level.CurrentLevel == null)
            throw new Exception("Level.CurrentLevel was null when trying to spawn an NPC. Can't spawn without a level.");

        DebugDraw.Cross(spawnPoint, 320, Colors.Magenta, 1, RenderOrders.Effects);
        DebugDraw.Circle(spawnPoint, 320, Colors.Magenta, 1, RenderOrders.Effects);

        // (duston) IMPORTANT: always set the initial position to spawnPoint first no matter what,
        // i don't care if its in the sky. only then, afterwards will we set the position to onGround.
        // this ensures that the matrices are being calculated at the right location first.

        // (zooi): unless they are spawned from a door :)

        var floorPos = new Vector2(spawnPoint.X, Level.CurrentLevel.GetFloorLevelAt(spawnPoint.X));

        CharacterComponent character;
        if (Utilities.RandomFloat() > weaponChance)
            character = Prefabs.CreateEnemy(Scene, spawnPoint, toSpawn.Stats, toSpawn.Look, toSpawn.Faction);
        else
            character = Prefabs.CreateEnemyWithWeapon(
                Scene,
                spawnPoint,
                (weapons == null || !weapons.Any()) ? null : Registries.Weapons.Get(weapons.ElementAt(Utilities.RandomInt(0, weapons.Count()))),
                toSpawn.Stats, toSpawn.Look, toSpawn.Faction);

        var floorOffset = CharacterConstants.GetFloorOffset(character.Positioning.Scale);
        var charOnFloorPos = new Vector2(floorPos.X, floorPos.Y + floorOffset);
        var finalSpawnPoint = applicableDoor == null ? spawnPoint : new Vector2(spawnPoint.X, spawnPoint.Y + floorOffset);

        character.Positioning.GlobalCenter = charOnFloorPos;
        character.Positioning.GlobalTarget = charOnFloorPos with { Y = 0 };

        if (spawnPoint.Y - charOnFloorPos.Y > 1000) // the spawner is way up in the air so we should play an animation
            character.PlayAnimation(Animations.SpawnFromSky);
        else if (applicableDoor != null)
        {
            character.Tint = Colors.Black;
            var door = applicableDoor;
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

    private DoorComponent? GetDoorNearest(Vector2 point)
    {
        var door = Scene.GetAllComponentsOfType<DoorComponent>();
        float minDistance = float.MaxValue;
        DoorComponent? nearest = null;
        foreach (var item in door)
        {
            if (!item.Properties.EnemySpawnerDoor)
                continue;
            var d = Vector2.DistanceSquared(item.Properties.SpawnPoint, point);
            if (d < minDistance)
            {
                minDistance = d;
                nearest = item;
                if (d <= float.Epsilon) // zo vroeg mogelijk he
                    return nearest;
            }
        }
        return nearest;
    }

    private static Vector2 GetRandomSpawnPoint(IList<Vector2> points, IList<Door> doors, out bool isDoor)
    {
        isDoor = false;
        bool hasAdditionalSpawnPoints = points.Any();
        bool hasDoors = doors.Any(static d => d.Properties.EnemySpawnerDoor);

        //geen spawnpoints en geen deuren
        if (!hasAdditionalSpawnPoints && !hasDoors)
            return default;

        //alleen spawnpoints
        if (hasAdditionalSpawnPoints && !hasDoors)
            return MadnessUtils.PickRandom(points);

        //alleen deuren
        if (!hasAdditionalSpawnPoints && hasDoors)
        {
            isDoor = true;
            //TODO dit kan sneller ook
            return MadnessUtils.PickRandom(doors).Properties.SpawnPoint;
        }

        // spawnpoints en deuren
        // Dit moet zo omdat nullable foutjes
        if (points.Any() && hasDoors)
        {
            float ratio = doors.Count / (float)(points.Count + doors.Count);
            if (Utilities.RandomFloat() < ratio) // make sure we weigh the selection appropriately
            {
                isDoor = true;
                return Utilities.PickRandom(doors).Properties.SpawnPoint;
            }
            else
                return Utilities.PickRandom(points);
        }

        return default;
    }

    public void Dispose()
    {
    }
}
