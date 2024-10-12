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
    private readonly List<Routine> routines = [];

    public override void OnDeactivate()
    {
        foreach (var r in routines)
            RoutineScheduler.Stop(r);
        routines.Clear();
    }

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        if (!Scene.FindAnyComponent<EnemySpawningComponent>(out var spawningComponent) || !spawningComponent.Enabled)
            return;

        var hasLevelProgress = Scene.FindAnyComponent<LevelProgressComponent>(out var lvlProgress);

        if (!CanSpawnAnotherEnemy(spawningComponent, lvlProgress, 1, out var liveEnemyCount))
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
            var spawnPoint = GetRandomSpawnPoint(spawningComponent, out var isDoor);
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
                    int amountToSpawn = GetNumberOfEnemiesToSpawn(spawningComponent, liveEnemyCount, lvlProgress);
                    routines.Add(RoutineScheduler.Start(DoorSpawnRoutine(amountToSpawn, weaponChance, door, enemies, weapons, spawnPoint)));
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
    {
        return (enemies == null || enemies.Count == 0) ? null : Utilities.PickRandom(enemies);
    }

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
        yield return new GameSafeRoutineDelay(DoorComponent.AnimationTime * 1.5f);

        for (int i = 0; i < amount; i++)
        {
            var instr = GetEnemySpawnInstructions(enemies);
            if (instr != null)
                Spawn(weaponChance, weapons, instr, spawnPoint, door);

            if (amount > 1)
                yield return new GameSafeRoutineDelay(Utilities.RandomFloat(0.1f, .4f));
        }

        yield return new GameSafeRoutineDelay(DoorComponent.AnimationTime * 0.5f);
        door.Close(Scene);
        door.Properties.IsPortal = wasPortal;
    }


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

    private int GetMaxEnemyCount(EnemySpawningComponent spawningComponent, int liveEnemyCount, LevelProgressComponent? lvlProgress)
    {
        if (lvlProgress != null && Level.CurrentLevel?.ProgressionType == ProgressionType.BodyCount)
            return int.Min(spawningComponent.MaxEnemyCount, lvlProgress.BodyCount.Target - lvlProgress.BodyCount.Current- liveEnemyCount);
        else
            return spawningComponent.MaxEnemyCount;
    }

    private int GetOpenDoorCount()
    {
        return Scene.GetAllComponentsOfType<DoorComponent>().Count(static d => d.IsOpen || d.IsBusyWithAnimation);
    }

    private bool CanSpawnAnotherEnemy(EnemySpawningComponent spawningComponent, LevelProgressComponent? lvlProgress, int requestedAmount, out int liveEnemyCount)
    {
        var enemyCount = GetLiveEnemyCount();
        var maxEnemyCount = GetMaxEnemyCount(spawningComponent, enemyCount, lvlProgress);
        var openDoors = GetOpenDoorCount();
        enemyCount += openDoors;
        liveEnemyCount = enemyCount;
        return enemyCount - maxEnemyCount - 1 < -requestedAmount; //-1 omdat uhhh
    }

    private int GetNumberOfEnemiesToSpawn(EnemySpawningComponent spawningComponent, int liveEnemyCount, LevelProgressComponent? lvlProgress)
    {
        var max = GetMaxEnemyCount(spawningComponent, liveEnemyCount, lvlProgress);
        if (max == 0)
            return 0;
        return Utilities.RandomInt(1, Math.Min(max, 3));
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

    private static Vector2 GetRandomSpawnPoint(EnemySpawningComponent spawningComponent, out bool isDoor)
    {
        var additionalSpawnPoints = spawningComponent.SpawnPoints ?? Array.Empty<Vector2>();
        var doors = spawningComponent.Doors ?? [];

        isDoor = false;
        bool hasAdditionalSpawnPoints = additionalSpawnPoints.Any();
        bool hasDoors = doors.Any(static d => d.Properties.EnemySpawnerDoor);

        //geen spawnpoints en geen deuren
        if (!hasAdditionalSpawnPoints && !hasDoors)
            return default;

        //alleen spawnpoints
        if (hasAdditionalSpawnPoints && !hasDoors)
            return MadnessUtils.PickRandom(additionalSpawnPoints);

        //alleen deuren
        if (!hasAdditionalSpawnPoints && hasDoors)
        {
            isDoor = true;
            //TODO dit kan sneller ook
            return MadnessUtils.PickRandom(doors).Properties.SpawnPoint;
        }

        // spawnpoints en deuren
        // Dit moet zo omdat nullable foutjes
        if (additionalSpawnPoints.Any() && hasDoors)
        {
            float ratio = doors.Count / (float)(additionalSpawnPoints.Count + doors.Count);
            if (Utilities.RandomFloat() < ratio) // make sure we weigh the selection appropriately
            {
                isDoor = true;
                return Utilities.PickRandom(doors).Properties.SpawnPoint;
            }
            else
                return Utilities.PickRandom(additionalSpawnPoints);
        }

        return default;
    }

    public void Dispose()
    {
    }
}
