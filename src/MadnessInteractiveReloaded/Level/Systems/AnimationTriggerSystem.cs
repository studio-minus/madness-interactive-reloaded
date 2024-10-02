using System.Numerics;
using Walgelijk;
using static MIR.AnimationTriggerComponent;

namespace MIR;

/// <summary>
/// Plays an animation when the player character overlaps a 
/// <see cref="AnimationTriggerComponent"/>'s bounds.
/// </summary>
public class AnimationTriggerSystem : Walgelijk.System
{
    public override void Update()
    {
        if (!MadnessUtils.FindPlayer(Scene, out var player, out var playerChar))
            return;

        if (player.IsDoingDyingSequence || !playerChar.IsAlive || playerChar.Positioning.IsFlying || playerChar.IsPlayingAnimation)
            return;

        var playerBounds = playerChar.GetBoundingBox(Scene);

        foreach (var trigger in Scene.GetAllComponentsOfType<AnimationTriggerComponent>())
        {
            if (trigger.StartEvent == TriggerType.LevelComplete)
            {
                if (!Scene.FindAnyComponent<LevelProgressComponent>(out var progress) || !progress.GoalReached)
                    continue;
            }

            if (!trigger.IsExpired && trigger.WorldRect.IntersectsRectangle(playerBounds))
            {
                switch (trigger.Target)
                {
                    case TargetType.Nearest:
                        {
                            var nearest = GetNearest(trigger.WorldRect.GetCenter());

                            if (nearest != null)
                                PlayFor(trigger, nearest);
                        }
                        break;
                    case TargetType.Player:
                        PlayFor(trigger, playerChar);
                        break;
                    case TargetType.CreateNew:
                        {
                            var look = string.IsNullOrWhiteSpace(trigger.CreateNewLook) ? Registries.Looks.GetRandomValue() : Registries.Looks.Get(trigger.CreateNewLook);
                            var stats = string.IsNullOrWhiteSpace(trigger.CreateNewStats) ? Registries.Stats.GetRandomValue() : Registries.Stats.Get(trigger.CreateNewStats);
                            var ch = Prefabs.CreateCharacter(Scene, new CharacterPrefabParams
                            {
                                Name = "Spawn of the Animation Trigger",
                                Bottom = trigger.TargetPosition,
                                Faction = Registries.Factions[trigger.CreateNewFaction],
                                Look = look,
                                Stats = stats,
                                Tag = Tags.EnemyAI
                            });

                            if (Registries.Factions.Get(trigger.CreateNewFaction).IsEnemiesWith(playerChar.Faction))
                            {
                                // TODO duplicate of Prefabs.CreateEnemy
                                Scene.AttachComponent(ch.Entity, new AiComponent());
                                var c = Scene.Id;
                                ch.OnDeath.AddListener(e =>
                                {
                                    if (Level.CurrentLevel != null 
                                        && Game.Main.Scene.Id == c 
                                        && Game.Main.Scene.FindAnyComponent<LevelProgressComponent>(out var progress))
                                            progress.BodyCount.Current++;
                                });
                            }

                            PlayFor(trigger, ch);
                        }
                        break;
                }
            }
        }
    }

    private void PlayFor(AnimationTriggerComponent trigger, CharacterComponent character)
    {
        trigger.IsExpired = true;

        var floorHeight = Level.CurrentLevel?.GetFloorLevelAt(trigger.TargetPosition.X) ?? 0;

        character.Positioning.GlobalTarget = trigger.TargetPosition;
        character.Positioning.GlobalTarget.Y -= floorHeight;
        character.Positioning.IsFlipped = trigger.TargetFlipped;
        character.Positioning.Feet.Second.Offset = default;
        character.Positioning.Feet.First.Offset = default;
        character.NeedsLookUpdate = true;
        character.PlayAnimation(Registries.Animations.Get(trigger.Animation));
    }

    private CharacterComponent? GetNearest(Vector2 position)
    {
        float minDist = float.MaxValue;
        CharacterComponent? nearest = null;
        foreach (var c in Scene.GetAllComponentsOfType<CharacterComponent>())
        {
            var d = Vector2.DistanceSquared(c.Positioning.GlobalCenter, position);
            if (d < minDist)
            {
                minDist = d;
                nearest = c;
            }
        }
        return nearest;
    }
}