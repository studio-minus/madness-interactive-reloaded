using System;
using Walgelijk;

namespace MIR;

/// <summary>
/// Manages <see cref="DoorComponent"/> doors.
/// </summary>
public class DoorSystem : Walgelijk.System
{
    public override void OnActivate()
    {
        foreach (var item in Scene.GetAllComponentsOfType<DoorComponent>())
        {
            item.IsBusyWithAnimation = false;
            item.Close(Scene);
        }
    }

    public override void Update()
    {
        if (MadnessUtils.IsPaused(Scene) || MadnessUtils.EditingInExperimentMode(Scene) || MadnessUtils.IsCutscenePlaying(Scene))
            return;

        MadnessUtils.FindPlayer(Scene, out var player, out var character);

        foreach (var item in Scene.GetAllComponentsOfType<DoorComponent>())
            ProcessDoor(item, player, character);
    }

    private void ProcessDoor(DoorComponent door, PlayerComponent? player, CharacterComponent? playerChar)
    {
        var material = door.Material;
        material.SetUniform(DoorComponent.TimeUniform, Time.SecondsSinceLoad);

        bool wantsToPassThrough =
            (playerChar?.IsAlive ?? false) &&
            !playerChar.Positioning.IsFlying &&
            (float.Sign(playerChar.WalkAcceleration.X) != float.Sign(door.Properties.FacingDirection.X)) &&
            float.Abs(playerChar.WalkAcceleration.X) > 50 &&
            (door.Properties.FacingDirection.X > 0 ? playerChar.Positioning.GlobalCenter.X < door.Properties.SpawnPoint.X : playerChar.Positioning.GlobalCenter.X > door.Properties.SpawnPoint.X);

        if (door.Properties.IsPortal)
        {
            if (!door.IsBusyWithAnimation)
            {
                if ((playerChar?.IsAlive ?? false) && !playerChar.Positioning.IsFlying && float.Abs(playerChar.Positioning.GlobalCenter.X - door.Properties.SpawnPoint.X) < DoorComponent.PortalOpeningDistance)
                    door.Open(Scene);
                else
                    door.Close(Scene);

                if (wantsToPassThrough && !string.IsNullOrWhiteSpace(door.Properties.DestinationLevel))
                {
                    if (!Registries.Levels.TryGet(door.Properties.DestinationLevel, out var dest))
                    {
#if DEBUG
                        throw new Exception($"{door.Properties.DestinationLevel} is not a valid level key");
#else   
                        Logger.Error($"{door.Properties.DestinationLevel} is not a valid level key");
                        return;
#endif
                    }

                    var sceneCacheId = door.Properties.DestinationLevel;
                    if (MadnessUtils.TransitionScene(game => LevelLoadingScene.Create(game, dest.Level, new SceneCacheSettings(sceneCacheId))))
                    {
                        SharedLevelData.TargetPortalID = door.Properties.PortalID;

                        if (playerChar != null)
                        {
                            if (playerChar.EquippedWeapon.TryGet(Scene, out var wpn) && wpn.RegistryKey != null)
                                SharedLevelData.EquippedWeaponPortal = new PersistentEquippedWeapon
                                {
                                    Key = wpn.RegistryKey,
                                    Ammo = wpn.RemainingRounds,
                                    InfiniteAmmo = wpn.InfiniteAmmo
                                };
                            else
                                SharedLevelData.EquippedWeaponPortal = default;
                        }

                        Logger.Log($"Equipped weapon for portal destination: {SharedLevelData.EquippedWeaponPortal} rounds");
                        Logger.Log($"Entered portal '{door.Properties.PortalID}', traveling to '{sceneCacheId}'");
                    }
                }
            }
        }
        else if (door.Properties.IsLevelProgressionDoor && door.IsOpen)
        {
            if (Scene.FindAnyComponent<LevelProgressComponent>(out var progress) && progress.GoalReached && wantsToPassThrough)
            {
                Scene.GetSystem<LevelProgressSystem>().TransitionToNextLevel();
                door.Properties.IsLevelProgressionDoor = false;
            }
        }
    }
}

