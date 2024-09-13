using MIR.LevelEditor;
using System;
using System.Linq;
using Walgelijk;

namespace MIR;

/// <summary>
/// Runs logic for the player's advancement through the level.
/// Such as number of kills required to pass.
/// </summary>
public class LevelProgressSystem : Walgelijk.System
{
    public override void Update()
    {
        if (Level.CurrentLevel == null || !Scene.FindAnyComponent<LevelProgressComponent>(out var progress))
            return;

        if (!MadnessUtils.FindPlayer(Scene, out var player, out var playerCharacter))
            return;

        if (MadnessUtils.IsPaused(Scene))
            return;

        var playerLives = playerCharacter.IsAlive && !player.IsDoingDyingSequence;

        if (playerLives)
        {
            CheckGoalReached(Level.CurrentLevel, progress);
        }

        if (Input.IsKeyReleased(Key.R) && !ImprobabilityDisks.IsEnabled("tricky"))
        {
            bool isDead = !playerLives;
            if (isDead && Scene.FindAnyComponent<PlayerDeathSequenceComponent>(out var deathSequence))
            {
                if (Scene.HasSystem<LevelEditorTestSystem>())
                    MadnessUtils.TransitionScene(g => LevelEditorTestScene.Create(g, Level.CurrentLevel));
                else
                {
                    if (Level.CurrentLevel != null)
                        MadnessUtils.TransitionScene(g => CampaignScene.Create(g, Level.CurrentLevel));
                    else
                        Logger.Warn("Attempt to reset level but the current level is null");
                }
            }
        }
    }

    private void CheckGoalReached(Level level, LevelProgressComponent progress)
    {
        if (Scene.FindAnyComponent<NeedsToDieComponent>(out _))
            return;

        switch (level.ProgressionType)
        {
            case ProgressionType.BodyCount:
                if (progress.BodyCount.Current >= progress.BodyCount.Target)
                    ForceReachGoal();
                break;
            case ProgressionType.Time:
                progress.Timed.Time += Time.DeltaTime;
                if (progress.Timed.Time >= level.TimeLimitInSeconds)
                    ForceReachGoal();
                break;
            case ProgressionType.Always:
                ForceReachGoal();
                break;
        }
    }

    public void ForceReachGoal()
    {
        if (!Scene.FindAnyComponent<LevelProgressComponent>(out var progress))
            throw new System.InvalidOperationException("There is no progress component in the scene, so you can't invoke Win");

        if (!progress.GoalReached)
            progress.GoalReached = true;

        // try opening the progress door
        var door = Scene.GetAllComponentsOfType<DoorComponent>()
            .FirstOrDefault(static d => d.Properties.IsLevelProgressionDoor);

        if (door != null)
            door.Open(Scene);
    }

    public void TransitionToNextLevel()
    {
        if (CampaignProgress.TryGetCurrentStats(out var stats))
        {
            int currentLevelIndex = Level.CurrentLevel == null ? 0 :
                Array.IndexOf(CampaignProgress.CurrentCampaign.Levels, Level.CurrentLevel.Id); // get index of current level

            if (CampaignProgress.GetCampaignLevelList().Length <= currentLevelIndex + 1)
            {
                // last level, you won the game
                // TODO stats ending screen?

                MadnessUtils.TransitionScene(game => MainMenuScene.Load(Game));
                if (PersistentSoundHandles.LevelMusic != null)
                    Audio.Stop(PersistentSoundHandles.LevelMusic);
                return;
            }

            if (Level.CurrentLevel == null || CampaignProgress.CurrentCampaign.Levels[stats.LevelIndexClamped] == Level.CurrentLevel.Id)
                CampaignProgress.SetProgressToNextLevel(); // only increment progression if we are playing the last unlocked level

            var nextLvl = Registries.Levels.Get(CampaignProgress.GetLevelKeyAt(currentLevelIndex + 1)
                ?? throw new Exception("Attempt to progress to next level at the end of the campaign level index "));

            PersistentEquippedWeapon? eqwt = default;

            if (MadnessUtils.FindPlayer(Scene, out _, out var character))
            {
                if (character.EquippedWeapon.TryGet(Scene, out var eqw) && eqw.RegistryKey != null)
                    eqwt = new PersistentEquippedWeapon
                    {
                        Ammo = eqw.RemainingRounds,
                        InfiniteAmmo = eqw.InfiniteAmmo,
                        Key = eqw.RegistryKey
                    };
            }
            else if (Level.CurrentLevel != null)
                eqwt = stats.ByLevel[Level.CurrentLevel.Id].EquippedWeapon; // propagate current equipped weapon stat if no player was found

            {
                var nextLevelStats = stats.ByLevel.Ensure(nextLvl.Id);
                nextLevelStats.EquippedWeapon = eqwt;
                stats.Save();
                Logger.Debug($"Equipped weapon for next level: {(eqwt?.Key ?? "None")} with {(eqwt?.Ammo ?? 0)} rounds");
            }

            if (nextLvl != null)
                MadnessUtils.TransitionScene(game => LevelLoadingScene.Create(game, nextLvl.Level, SceneCacheSettings.NoCache));
            else
                throw new InvalidOperationException("Attempt to progress to next level at the end of the campaign level index");
        }
        else
            throw new Exception("Campaign is missing a stats entry");
    }
}
