using System.Collections.Generic;
using Walgelijk;
using Walgelijk.Physics;

namespace MIR;

public class TeleportDashAbilityComponent : CharacterAbilityComponent
{
    public TeleportDashAbilityComponent(AbilitySlot slot) : base(slot, AbilityBehaviour.Hold)
    {
    }

    //TODO idk if this ability makes any sense ³¤
    public override string DisplayName => "Teleport";
    public float Distance = 1000;
    private bool flipFlop = false;
    private QueryResult[] buffer = new QueryResult[4];
    private bool routine = false;

    public override AnimationConstraint Constraints => routine ? 
        AnimationConstraint.PreventDying | AnimationConstraint.PreventAllMovement : 
        AnimationConstraint.AllowAll;

    public override void StartAbility(AbilityParams a)
    {

    }

    public override void UpdateAbility(AbilityParams a)
    {
        if (!IsUsing || a.Character.IsPlayingAnimation)
        {
            flipFlop = false;
            return;
        }

        var level = Level.CurrentLevel;
        if (level == null || routine)
            return;

        var phys = a.Scene.GetSystem<PhysicsSystem>();
        var l = CollisionLayers.BlockMovement | CollisionLayers.BlockPhysics;

        if (flipFlop && float.Abs(a.Character.WalkAcceleration.X) < 100)
        {
            flipFlop = false;
        }
        else if (!flipFlop && float.Abs(a.Character.WalkAcceleration.X) > 10)
        {
            flipFlop = true;
            var sign = float.Sign(a.Character.WalkAcceleration.X);
            var p = a.Character.Positioning;

            routine = true;

            RoutineScheduler.Start(Teleport());

            IEnumerator<IRoutineCommand> Teleport()
            {
                Audio.PlayOnce(Sounds.TrickyTeleport, 0.9f, Utilities.RandomFloat(0.9f, 1.1f), AudioTracks.SoundEffects);
                var a = p.GlobalCenter;
                var b = p.GlobalCenter;
                b.X = float.Clamp(b.X + Distance * sign, level.FloorLine[0].X, level.FloorLine[^1].X);
                b.Y = Level.CurrentLevel!.GetFloorLevelAt(b.X) + CharacterConstants.GetFloorOffset(p.Scale);

                float t = 0;
                const float timeDuration = 0.2f;
                while (true)
                {
                    yield return new GameSafeRoutineDelay();
                    t += Game.Main.State.Time.DeltaTime / timeDuration;
                    float f = Easings.Cubic.In(t);

                    p.GlobalCenter.X = float.Lerp(a.X, b.X, f);
                    p.GlobalCenter.Y = float.Lerp(a.Y, b.Y, f);

                    p.GlobalTarget = p.GlobalCenter;

                    p.HopStartingPosition = p.GlobalCenter.X;
                    p.NextHopPosition = p.GlobalCenter.X;
                    p.HopAnimationTimer = 0;

                    if (t > 1)
                        break;
                }

                routine = false;
            }
        }
    }

    public override void EndAbility(AbilityParams a)
    {
    }
}